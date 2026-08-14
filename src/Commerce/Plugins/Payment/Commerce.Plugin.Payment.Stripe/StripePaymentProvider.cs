using Commerce.Payments.Contracts.Callbacks;
using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Plugin.Payment.Stripe;

public sealed class StripePaymentProvider(
    IPaymentProviderSettingsReader settings,
    StripeApiClient apiClient,
    ILogger<StripePaymentProvider> logger) : IPaymentProvider
{
    public string ProviderSystemName => PaymentProviderNames.Stripe;

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var secretKey = await settings.GetAsync(StripeSettingKeys.SecretKey, request.StoreId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return Failed("configuration", "Stripe secret key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.ReturnUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
        {
            return Failed("configuration", "Return and cancel URLs are required for Stripe Checkout.");
        }

        if (!TryConvertAmount(request.Currency, request.Amount, out var minorUnits, out var amountError))
        {
            return Failed("invalid_amount", amountError!);
        }

        var successUrl = AppendQuery(request.ReturnUrl, "session_id", "{CHECKOUT_SESSION_ID}");
        var metadata = new Dictionary<string, string>
        {
            [StripeMetadataKeys.PaymentId] = request.PaymentId.ToString(),
            [StripeMetadataKeys.OrderId] = request.OrderId.ToString(),
            [StripeMetadataKeys.StoreId] = request.StoreId.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            metadata[StripeMetadataKeys.IdempotencyKey] = request.IdempotencyKey;
        }

        var session = await apiClient.CreateCheckoutSessionAsync(
            secretKey,
            minorUnits,
            request.Currency,
            successUrl,
            request.CancelUrl!,
            metadata,
            request.IdempotencyKey,
            cancellationToken).ConfigureAwait(false);

        if (!session.Success || string.IsNullOrWhiteSpace(session.SessionId) || string.IsNullOrWhiteSpace(session.Url))
        {
            return Failed("session_failed", session.ErrorMessage ?? "Stripe checkout session creation failed.");
        }

        logger.LogInformation(
            "Stripe checkout session {SessionId} created for payment {PaymentId}",
            session.SessionId,
            request.PaymentId);

        return new PaymentResult(
            Success: true,
            Status: PaymentStatus.RedirectRequired,
            ProviderPaymentId: session.PaymentIntentId ?? session.SessionId,
            RedirectUrl: session.Url);
    }

    public async Task<PaymentVerificationResult> GetPaymentStatusAsync(
        int paymentId,
        string? providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        return new PaymentVerificationResult(
            Success: false,
            Status: PaymentStatus.Initiated,
            ProviderPaymentId: providerPaymentId,
            FailureCode: "reconciliation_required",
            FailureMessage: "Use session verification or Stripe webhook for authoritative status.");
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        int paymentId,
        string? providerPaymentId,
        IReadOnlyDictionary<string, string>? callbackData = null,
        CancellationToken cancellationToken = default)
    {
        callbackData ??= new Dictionary<string, string>();

        if (string.Equals(GetValue(callbackData, "event_type"), "checkout.session.async_payment_failed", StringComparison.OrdinalIgnoreCase))
        {
            return FailedVerification("payment_failed", "Stripe async payment failed.");
        }

        var sessionId = GetValue(callbackData, "session_id")
            ?? GetValue(callbackData, "sessionId")
            ?? GetValue(callbackData, "checkout_session_id");

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return FailedVerification("missing_session", "Stripe session id is required for verification.");
        }

        var storeId = ParseStoreId(callbackData);
        if (!storeId.HasValue)
        {
            return FailedVerification("missing_store", "Store id is required for Stripe verification.");
        }

        var secretKey = await settings.GetAsync(StripeSettingKeys.SecretKey, storeId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return FailedVerification("configuration", "Stripe secret key is not configured.");
        }

        if (callbackData.TryGetValue("paymentId", out var paymentIdRaw) &&
            int.TryParse(paymentIdRaw, out var callbackPaymentId) &&
            callbackPaymentId != paymentId)
        {
            return FailedVerification("payment_mismatch", "Callback payment id does not match.");
        }

        var session = await apiClient.GetCheckoutSessionAsync(secretKey, sessionId, cancellationToken).ConfigureAwait(false);
        if (!session.Success)
        {
            return FailedVerification("provider_error", session.ErrorMessage ?? "Stripe session lookup failed.");
        }

        return MapSessionStatus(session, providerPaymentId ?? session.PaymentIntentId ?? sessionId);
    }

    public async Task<PaymentResult> CaptureAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var secretKey = await settings.GetAsync(StripeSettingKeys.SecretKey, request.StoreId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(request.ProviderPaymentId))
        {
            return Failed("configuration", "Stripe capture requires configured secret key and payment intent id.");
        }

        var capture = await apiClient
            .CapturePaymentIntentAsync(secretKey, request.ProviderPaymentId, cancellationToken)
            .ConfigureAwait(false);

        return capture.Success
            ? new PaymentResult(Success: true, Status: PaymentStatus.Captured, ProviderPaymentId: request.ProviderPaymentId)
            : Failed("capture_failed", capture.ErrorMessage ?? "Stripe capture failed.");
    }

    public async Task<PaymentResult> VoidAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var secretKey = await settings.GetAsync(StripeSettingKeys.SecretKey, request.StoreId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(request.ProviderPaymentId))
        {
            return Failed("configuration", "Stripe void requires configured secret key and payment intent id.");
        }

        var cancel = await apiClient
            .CancelPaymentIntentAsync(secretKey, request.ProviderPaymentId, cancellationToken)
            .ConfigureAwait(false);

        return cancel.Success
            ? new PaymentResult(Success: true, Status: PaymentStatus.Cancelled, ProviderPaymentId: request.ProviderPaymentId)
            : Failed("void_failed", cancel.ErrorMessage ?? "Stripe void failed.");
    }

    public async Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        var secretKey = await settings.GetAsync(StripeSettingKeys.SecretKey, request.StoreId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new RefundResult(false, RefundStatus.Failed, FailureCode: "configuration", FailureMessage: "Stripe secret key is not configured.");
        }

        if (!TryConvertAmount(request.Currency, request.Amount, out var minorUnits, out var amountError))
        {
            return new RefundResult(false, RefundStatus.Failed, FailureCode: "invalid_amount", FailureMessage: amountError);
        }

        var paymentIntentId = request.ProviderPaymentId;
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            return new RefundResult(false, RefundStatus.Failed, FailureCode: "missing_intent", FailureMessage: "Payment intent id is required for Stripe refunds.");
        }

        var refund = await apiClient
            .CreateRefundAsync(secretKey, paymentIntentId, minorUnits, request.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        return refund.Success
            ? new RefundResult(true, RefundStatus.Succeeded, refund.RefundId)
            : new RefundResult(false, RefundStatus.Failed, FailureCode: "refund_failed", FailureMessage: refund.ErrorMessage);
    }

    internal static PaymentVerificationResult MapSessionStatus(
        StripeSessionStatusResult session,
        string providerPaymentId)
    {
        if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentVerificationResult(
                Success: true,
                Status: PaymentStatus.Captured,
                ProviderPaymentId: providerPaymentId);
        }

        if (string.Equals(session.Status, "expired", StringComparison.OrdinalIgnoreCase))
        {
            return FailedVerification("expired", "Stripe checkout session expired.", PaymentStatus.Cancelled);
        }

        if (string.Equals(session.PaymentStatus, "unpaid", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentVerificationResult(
                Success: false,
                Status: PaymentStatus.Initiated,
                ProviderPaymentId: providerPaymentId,
                FailureCode: "unknown_state",
                FailureMessage: "Stripe session is unpaid — awaiting webhook or customer action.");
        }

        return FailedVerification(
            "unknown_state",
            $"Unhandled Stripe session status payment={session.PaymentStatus} session={session.Status}",
            PaymentStatus.Initiated);
    }

    private static bool TryConvertAmount(string currency, decimal amount, out long minorUnits, out string? error)
    {
        if (amount <= 0)
        {
            minorUnits = 0;
            error = "Amount must be greater than zero.";
            return false;
        }

        minorUnits = (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        if (minorUnits <= 0)
        {
            error = "Amount is too small for Stripe.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            error = "Currency is required.";
            return false;
        }

        error = null;
        return true;
    }

    private static PaymentResult Failed(string code, string message) =>
        new(Success: false, Status: PaymentStatus.Failed, FailureCode: code, FailureMessage: message);

    private static PaymentVerificationResult FailedVerification(
        string code,
        string message,
        PaymentStatus status = PaymentStatus.Failed) =>
        new(Success: false, Status: status, FailureCode: code, FailureMessage: message);

    private static string? GetValue(IReadOnlyDictionary<string, string> data, string key) =>
        data.TryGetValue(key, out var value) ? value : null;

    private static int? ParseStoreId(IReadOnlyDictionary<string, string> data) =>
        data.TryGetValue("storeId", out var storeIdValue) && int.TryParse(storeIdValue, out var storeId)
            ? storeId
            : data.TryGetValue(StripeMetadataKeys.StoreId, out var metaStoreId) && int.TryParse(metaStoreId, out storeId)
                ? storeId
                : null;

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}

public sealed class StripeCallbackHandler(
    IPaymentProviderSettingsReader settings,
    IPaymentService paymentService,
    ILogger<StripeCallbackHandler> logger) : IPaymentCallbackHandler
{
    public string ProviderSystemName => PaymentProviderNames.Stripe;

    public async Task<Commerce.Framework.Core.Results.Result<int?>> HandleAsync(
        PaymentCallbackContext context,
        CancellationToken cancellationToken = default)
    {
        context.Data.TryGetValue("body", out var payload);
        payload ??= string.Empty;

        var signature = context.Headers?.GetValueOrDefault("Stripe-Signature")
            ?? context.Headers?.GetValueOrDefault("stripe-signature");

        if (string.IsNullOrWhiteSpace(signature))
        {
            return await HandleBrowserReturnAsync(context, cancellationToken).ConfigureAwait(false);
        }

        return await HandleWebhookAsync(context, payload, signature, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Commerce.Framework.Core.Results.Result<int?>> HandleBrowserReturnAsync(
        PaymentCallbackContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Data.TryGetValue("paymentId", out var paymentIdValue) ||
            !int.TryParse(paymentIdValue, out var paymentId))
        {
            return Commerce.Framework.Core.Results.Result.Failure<int?>(
                Commerce.Framework.Core.Errors.Error.Validation("paymentId is required for Stripe return verification."));
        }

        var paymentResult = await paymentService.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (!paymentResult.IsSuccess)
        {
            return Commerce.Framework.Core.Results.Result.Failure<int?>(paymentResult.Error!);
        }

        var payment = paymentResult.Value!.Payment;
        var enriched = new Dictionary<string, string>(context.Data, StringComparer.OrdinalIgnoreCase)
        {
            ["paymentId"] = paymentId.ToString(),
            ["storeId"] = payment.StoreId.ToString()
        };

        var verifyResult = await paymentService.ProcessCallbackAsync(
            context.ProviderSystemName,
            context.CallbackKey,
            context.PayloadHash,
            enriched,
            cancellationToken).ConfigureAwait(false);

        return verifyResult.IsSuccess
            ? Commerce.Framework.Core.Results.Result.Success<int?>(paymentId)
            : Commerce.Framework.Core.Results.Result.Failure<int?>(verifyResult.Error!);
    }

    private async Task<Commerce.Framework.Core.Results.Result<int?>> HandleWebhookAsync(
        PaymentCallbackContext context,
        string payload,
        string signature,
        CancellationToken cancellationToken)
    {
        using var json = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
        var root = json.RootElement;
        var eventType = root.GetProperty("type").GetString();

        if (!string.Equals(eventType, "checkout.session.completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(eventType, "checkout.session.async_payment_failed", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Ignoring Stripe webhook event {EventType}", eventType);
            return Commerce.Framework.Core.Results.Result.Success<int?>(null);
        }

        var session = root.GetProperty("data").GetProperty("object");
        var metadata = session.GetProperty("metadata");
        var paymentIdRaw = metadata.GetProperty(StripeMetadataKeys.PaymentId).GetString();
        var storeIdRaw = metadata.GetProperty(StripeMetadataKeys.StoreId).GetString();

        if (!int.TryParse(paymentIdRaw, out var paymentId) || !int.TryParse(storeIdRaw, out var storeId))
        {
            return Commerce.Framework.Core.Results.Result.Failure<int?>(
                Commerce.Framework.Core.Errors.Error.Validation("Stripe webhook metadata is missing commerce payment identifiers."));
        }

        var webhookSecret = await settings.GetAsync(StripeSettingKeys.WebhookSecret, storeId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return Commerce.Framework.Core.Results.Result.Failure<int?>(
                Commerce.Framework.Core.Errors.Error.Validation("Stripe webhook secret is not configured."));
        }

        if (!StripeApiClient.VerifyWebhookSignature(payload, signature, webhookSecret, out var signatureError))
        {
            logger.LogWarning("Stripe webhook signature rejected: {Reason}", signatureError);
            return Commerce.Framework.Core.Results.Result.Failure<int?>(
                Commerce.Framework.Core.Errors.Error.Validation(signatureError ?? "Invalid Stripe webhook signature."));
        }

        var sessionId = session.GetProperty("id").GetString();
        var enriched = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["paymentId"] = paymentId.ToString(),
            ["storeId"] = storeId.ToString(),
            ["session_id"] = sessionId ?? string.Empty,
            ["event_type"] = eventType ?? string.Empty
        };

        var verifyResult = await paymentService.ProcessCallbackAsync(
            context.ProviderSystemName,
            context.CallbackKey,
            context.PayloadHash,
            enriched,
            cancellationToken).ConfigureAwait(false);

        return verifyResult.IsSuccess
            ? Commerce.Framework.Core.Results.Result.Success<int?>(paymentId)
            : Commerce.Framework.Core.Results.Result.Failure<int?>(verifyResult.Error!);
    }
}
