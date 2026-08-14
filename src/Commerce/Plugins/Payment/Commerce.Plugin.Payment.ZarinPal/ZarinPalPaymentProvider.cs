using Commerce.Payments.Contracts.Callbacks;
using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Plugin.Payment.ZarinPal;

public sealed class ZarinPalPaymentProvider(
    IPaymentProviderSettingsReader settings,
    ZarinPalApiClient apiClient,
    ILogger<ZarinPalPaymentProvider> logger) : IPaymentProvider
{
    public string ProviderSystemName => PaymentProviderNames.ZarinPal;

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var merchantId = await settings.GetAsync(ZarinPalSettingKeys.MerchantId, request.StoreId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return Failed("configuration", "ZarinPal merchant id is not configured.");
        }

        if (!TryConvertAmount(request.Currency, request.Amount, out var amountInRials, out var amountError))
        {
            return Failed("invalid_amount", amountError!);
        }

        var sandbox = await settings.GetBoolAsync(ZarinPalSettingKeys.Sandbox, request.StoreId, true, cancellationToken)
            .ConfigureAwait(false);
        var apiBase = sandbox ? ZarinPalEndpoints.SandboxApi : ZarinPalEndpoints.ProductionApi;
        var startPayBase = sandbox ? ZarinPalEndpoints.SandboxStartPay : ZarinPalEndpoints.ProductionStartPay;

        var callbackBase = await settings.GetAsync(ZarinPalSettingKeys.CallbackBaseUrl, request.StoreId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(callbackBase))
        {
            return Failed("configuration", "ZarinPal callback base URL is not configured.");
        }

        var callbackUrl = AppendQuery(callbackBase.TrimEnd('/'), "paymentId", request.PaymentId.ToString());
        var description = $"Order {request.OrderId} / Payment {request.PaymentId}";

        var result = await apiClient
            .RequestPaymentAsync(apiBase, merchantId, amountInRials, callbackUrl, description, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Authority))
        {
            return Failed(result.FailureCode ?? "request_failed", result.FailureMessage ?? "ZarinPal payment request failed.");
        }

        logger.LogInformation(
            "ZarinPal payment requested for payment {PaymentId}, authority {Authority}",
            request.PaymentId,
            result.Authority);

        return new PaymentResult(
            Success: true,
            Status: PaymentStatus.RedirectRequired,
            ProviderPaymentId: result.Authority,
            RedirectUrl: $"{startPayBase}/{result.Authority}");
    }

    public async Task<PaymentVerificationResult> GetPaymentStatusAsync(
        int paymentId,
        string? providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            return Unknown("missing_authority", "Provider authority is missing.");
        }

        return new PaymentVerificationResult(
            Success: false,
            Status: PaymentStatus.Initiated,
            ProviderPaymentId: providerPaymentId,
            FailureCode: "reconciliation_required",
            FailureMessage: "Use callback verification or admin reconciliation for ZarinPal status.");
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        int paymentId,
        string? providerPaymentId,
        IReadOnlyDictionary<string, string>? callbackData = null,
        CancellationToken cancellationToken = default)
    {
        callbackData ??= new Dictionary<string, string>();
        var authority = providerPaymentId
            ?? GetValue(callbackData, "Authority")
            ?? GetValue(callbackData, "authority");

        if (string.IsNullOrWhiteSpace(authority))
        {
            return FailedVerification("missing_authority", "ZarinPal authority is required.");
        }

        if (!callbackData.TryGetValue("paymentId", out var paymentIdValue) ||
            !int.TryParse(paymentIdValue, out var callbackPaymentId) ||
            callbackPaymentId != paymentId)
        {
            return FailedVerification("payment_mismatch", "Callback payment id does not match.");
        }

        var storeId = ParseStoreId(callbackData);
        if (!storeId.HasValue)
        {
            return FailedVerification("missing_store", "Store id is required for verification.");
        }

        var merchantId = await settings.GetAsync(ZarinPalSettingKeys.MerchantId, storeId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return FailedVerification("configuration", "ZarinPal merchant id is not configured.");
        }

        var browserStatus = GetValue(callbackData, "Status") ?? GetValue(callbackData, "status");
        if (string.Equals(browserStatus, "NOK", StringComparison.OrdinalIgnoreCase))
        {
            return FailedVerification("cancelled", "Customer cancelled payment at ZarinPal.", PaymentStatus.Cancelled);
        }

        if (!callbackData.TryGetValue("amount", out var amountRaw) ||
            !decimal.TryParse(amountRaw, out var amount) ||
            !callbackData.TryGetValue("currency", out var currency))
        {
            return FailedVerification("missing_amount", "Payment amount and currency are required for server verification.");
        }

        if (!TryConvertAmount(currency, amount, out var amountInRials, out var amountError))
        {
            return FailedVerification("invalid_amount", amountError!);
        }

        var sandbox = await settings.GetBoolAsync(ZarinPalSettingKeys.Sandbox, storeId.Value, true, cancellationToken)
            .ConfigureAwait(false);
        var apiBase = sandbox ? ZarinPalEndpoints.SandboxApi : ZarinPalEndpoints.ProductionApi;

        var verify = await apiClient
            .VerifyPaymentAsync(apiBase, merchantId, amountInRials, authority, cancellationToken)
            .ConfigureAwait(false);

        if (!verify.Success)
        {
            logger.LogWarning(
                "ZarinPal verification failed for payment {PaymentId}, authority {Authority}: {Message}",
                paymentId,
                authority,
                verify.FailureMessage);

            return FailedVerification(
                verify.FailureCode ?? "verification_failed",
                verify.FailureMessage ?? "ZarinPal verification failed.");
        }

        logger.LogInformation(
            "ZarinPal payment verified for payment {PaymentId}, ref {RefId}, alreadyVerified={AlreadyVerified}",
            paymentId,
            verify.RefId,
            verify.AlreadyVerified);

        return new PaymentVerificationResult(
            Success: true,
            Status: PaymentStatus.Captured,
            ProviderPaymentId: verify.RefId ?? authority);
    }

    public Task<PaymentResult> CaptureAsync(PaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentResult(
            Success: true,
            Status: PaymentStatus.Captured,
            ProviderPaymentId: request.PaymentMethodSystemName,
            Instructions: "ZarinPal captures on successful verification."));

    public Task<PaymentResult> VoidAsync(PaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed("not_supported", "ZarinPal void is not supported after redirect initiation."));

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RefundResult(
            Success: false,
            Status: RefundStatus.Failed,
            FailureCode: "not_supported",
            FailureMessage: "ZarinPal refunds must be processed in the ZarinPal merchant panel for this plugin version."));

    private static bool TryConvertAmount(string currency, decimal amount, out long amountInRials, out string? error)
    {
        if (!string.Equals(currency, "IRR", StringComparison.OrdinalIgnoreCase))
        {
            amountInRials = 0;
            error = "ZarinPal requires IRR currency amounts in Rials.";
            return false;
        }

        if (amount <= 0 || amount != decimal.Truncate(amount))
        {
            amountInRials = 0;
            error = "ZarinPal amount must be a positive whole number of Rials.";
            return false;
        }

        amountInRials = (long)amount;
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

    private static PaymentVerificationResult Unknown(string code, string message) =>
        new(Success: false, Status: PaymentStatus.Initiated, FailureCode: code, FailureMessage: message);

    private static string? GetValue(IReadOnlyDictionary<string, string> data, string key) =>
        data.TryGetValue(key, out var value) ? value : null;

    private static int? ParseStoreId(IReadOnlyDictionary<string, string> data) =>
        data.TryGetValue("storeId", out var storeIdValue) && int.TryParse(storeIdValue, out var storeId)
            ? storeId
            : null;

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}

public sealed class ZarinPalCallbackHandler(
    IPaymentService paymentService,
    ILogger<ZarinPalCallbackHandler> logger) : IPaymentCallbackHandler
{
    public string ProviderSystemName => PaymentProviderNames.ZarinPal;

    public async Task<Commerce.Framework.Core.Results.Result<int?>> HandleAsync(
        PaymentCallbackContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.Data.TryGetValue("paymentId", out var paymentIdValue) ||
            !int.TryParse(paymentIdValue, out var paymentId))
        {
            return Commerce.Framework.Core.Results.Result.Failure<int?>(
                Commerce.Framework.Core.Errors.Error.Validation("paymentId query parameter is required."));
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
            ["storeId"] = payment.StoreId.ToString(),
            ["amount"] = payment.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["currency"] = payment.Currency
        };

        if (!enriched.ContainsKey("Authority") && !enriched.ContainsKey("authority") &&
            !string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            enriched["Authority"] = payment.ProviderPaymentId!;
        }

        var verifyResult = await paymentService.ProcessCallbackAsync(
            context.ProviderSystemName,
            context.CallbackKey,
            context.PayloadHash,
            enriched,
            cancellationToken).ConfigureAwait(false);

        if (!verifyResult.IsSuccess)
        {
            logger.LogWarning(
                "ZarinPal callback verification failed for payment {PaymentId}: {Error}",
                paymentId,
                verifyResult.Error?.Message);
            return Commerce.Framework.Core.Results.Result.Failure<int?>(verifyResult.Error!);
        }

        return Commerce.Framework.Core.Results.Result.Success<int?>(paymentId);
    }
}
