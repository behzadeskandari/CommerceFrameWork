using Commerce.Framework.Application.Observability;
using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Commerce.Payments.Application.Abstractions;
using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Domain.Entities;
using Commerce.Payments.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Payments.Application.Payments;

public sealed class PaymentService(
    IPaymentRepository repository,
    IOrderPaymentSyncRepository orderRepository,
    PaymentProviderResolver providerResolver,
    IOrderPaymentSyncService orderPaymentSyncService,
    IStoreContext storeContext,
    IAuditPublisher auditPublisher,
    ICorrelationContext correlationContext,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<Result<CreatePaymentResultDto>> CreateForOrderAsync(
        CreatePaymentForOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.StoreMismatch());
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.IdempotencyKeyRequired());
        }

        var normalizedKey = idempotencyKey.Trim();
        var existingByKey = await repository
            .GetByIdempotencyKeyAsync(storeId.Value, normalizedKey, cancellationToken)
            .ConfigureAwait(false);

        if (existingByKey is not null)
        {
            return Result.Success(PaymentMapper.ToCreateResult(existingByKey));
        }

        var existingByOrder = await repository.GetByOrderIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (existingByOrder is not null)
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.OrderPaymentAlreadyExists(request.OrderId));
        }

        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.OrderNotFound(request.OrderId));
        }

        if (order.StoreId != storeId.Value)
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.StoreMismatch());
        }

        var methodSystemName = request.PaymentMethodSystemName
            ?? order.SelectedPaymentMethodSystemName
            ?? (order.GrandTotal == 0 ? PaymentProviderNames.FreeMethod : null);

        if (string.IsNullOrWhiteSpace(methodSystemName))
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.PaymentMethodNotFound());
        }

        var method = await ResolveMethodAsync(
            storeId.Value,
            request.PaymentMethodId ?? order.SelectedPaymentMethodId,
            methodSystemName,
            cancellationToken).ConfigureAwait(false);

        if (method is null)
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.PaymentMethodNotFound());
        }

        if (order.GrandTotal == 0 && !method.SupportsFreeOrders &&
            !string.Equals(method.SystemName, PaymentProviderNames.FreeMethod, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.PaymentMethodNotFound());
        }

        IPaymentProvider provider;
        try
        {
            provider = providerResolver.Resolve(method.ProviderSystemName);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.ProviderNotFound(method.ProviderSystemName));
        }

        var payment = Payment.Create(
            order.StoreId,
            order.Id,
            order.CustomerId,
            order.CurrencyCode,
            order.GrandTotal,
            method.ProviderSystemName,
            normalizedKey);

        await repository.AddAsync(payment, cancellationToken).ConfigureAwait(false);

        var attemptNumber = payment.Attempts.Count + 1;
        var attempt = payment.StartAttempt(attemptNumber);

        var providerRequest = new PaymentRequest(
            payment.Id,
            payment.StoreId,
            payment.OrderId,
            payment.CustomerId,
            payment.Currency,
            payment.Amount,
            method.SystemName,
            request.ReturnUrl,
            request.CancelUrl,
            normalizedKey,
            payment.ProviderPaymentId);

        PaymentResult providerResult;
        try
        {
            providerResult = await provider.CreatePaymentAsync(providerRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment provider {Provider} failed for order {OrderId}", method.ProviderSystemName, order.Id);
            attempt.MarkFailed(ex.Message);
            payment.MarkFailed(ex.Message);
            payment.AddTransaction(
                PaymentTransactionType.Sale,
                payment.Amount,
                payment.Currency,
                PaymentTransactionStatus.Failed,
                failureMessage: ex.Message);
            await repository.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
            await orderPaymentSyncService.SyncFailedAsync(order.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            return Result.Failure<CreatePaymentResultDto>(PaymentErrors.InvalidPaymentState(ex.Message));
        }

        ApplyProviderResult(payment, attempt, providerResult, PaymentTransactionType.Sale);
        await repository.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
        await SyncOrderFromPaymentStatusAsync(payment, cancellationToken).ConfigureAwait(false);

        using (CommerceLogging.BeginOperationScope(
            logger,
            correlationContext,
            "payment.created",
            ("PaymentId", payment.Id),
            ("OrderId", order.Id),
            ("Status", payment.Status.ToString())))
        {
            CommerceMetrics.PaymentOperations.Add(1, new KeyValuePair<string, object?>("operation", "created"));
            logger.LogInformation(
                "Payment {PaymentId} created for order {OrderId} with status {Status}",
                payment.Id,
                order.Id,
                payment.Status);
        }

        return Result.Success(PaymentMapper.ToCreateResult(payment, providerResult));
    }

    public async Task<Result<PaymentDetailDto>> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await repository.GetByIdWithDetailsAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null || !IsStoreAccessible(payment.StoreId))
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.PaymentNotFound(paymentId));
        }

        return Result.Success(PaymentMapper.ToDetail(payment));
    }

    public async Task<Result<PaymentDetailDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var payment = await repository.GetByOrderIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (payment is null || !IsStoreAccessible(payment.StoreId))
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.PaymentNotFoundForOrder(orderId));
        }

        return Result.Success(PaymentMapper.ToDetail(payment));
    }

    public async Task<Result<PaymentDetailDto>> ProcessCallbackAsync(
        string providerSystemName,
        string callbackKey,
        string payloadHash,
        IReadOnlyDictionary<string, string> callbackData,
        CancellationToken cancellationToken = default)
    {
        var existingCallback = await repository
            .GetCallbackRecordAsync(providerSystemName, callbackKey, cancellationToken)
            .ConfigureAwait(false);

        if (existingCallback is not null)
        {
            if (existingCallback.PaymentId.HasValue)
            {
                var existingPayment = await repository
                    .GetByIdWithDetailsAsync(existingCallback.PaymentId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (existingPayment is not null)
                {
                    return Result.Success(PaymentMapper.ToDetail(existingPayment));
                }
            }

            return Result.Failure<PaymentDetailDto>(PaymentErrors.CallbackAlreadyProcessed());
        }

        if (!providerResolver.TryResolve(providerSystemName, out var provider) || provider is null)
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.ProviderNotFound(providerSystemName));
        }

        if (!callbackData.TryGetValue("paymentId", out var paymentIdValue) ||
            !int.TryParse(paymentIdValue, out var paymentId))
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.InvalidPaymentState("Payment id is required in callback."));
        }

        var payment = await repository.GetByIdWithDetailsAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.PaymentNotFound(paymentId));
        }

        var verification = await provider.VerifyPaymentAsync(
            payment.Id,
            payment.ProviderPaymentId,
            callbackData,
            cancellationToken).ConfigureAwait(false);

        ApplyVerificationResult(payment, verification);
        await repository.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
        await SyncOrderFromPaymentStatusAsync(payment, cancellationToken).ConfigureAwait(false);

        await repository.AddCallbackRecordAsync(
            PaymentCallbackRecord.Create(providerSystemName, callbackKey, payloadHash, payment.Id),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(PaymentMapper.ToDetail(payment));
    }

    public async Task<Result<PaymentDetailDto>> CaptureAsync(int paymentId, CancellationToken cancellationToken = default) =>
        await ExecuteProviderActionAsync(
            paymentId,
            PaymentTransactionType.Capture,
            (provider, payment, methodSystemName) => provider.CaptureAsync(
                new PaymentRequest(
                    payment.Id,
                    payment.StoreId,
                    payment.OrderId,
                    payment.CustomerId,
                    payment.Currency,
                    payment.Amount,
                    methodSystemName,
                    ProviderPaymentId: payment.ProviderPaymentId),
                cancellationToken),
            cancellationToken).ConfigureAwait(false);

    public async Task<Result<PaymentDetailDto>> VoidAsync(int paymentId, CancellationToken cancellationToken = default) =>
        await ExecuteProviderActionAsync(
            paymentId,
            PaymentTransactionType.Void,
            (provider, payment, methodSystemName) => provider.VoidAsync(
                new PaymentRequest(
                    payment.Id,
                    payment.StoreId,
                    payment.OrderId,
                    payment.CustomerId,
                    payment.Currency,
                    payment.Amount,
                    methodSystemName,
                    ProviderPaymentId: payment.ProviderPaymentId),
                cancellationToken),
            cancellationToken).ConfigureAwait(false);

    public Task<Result<PaymentDetailDto>> RefundAsync(
        int paymentId,
        string? reason,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var paymentTask = repository.GetByIdWithDetailsAsync(paymentId, cancellationToken);
        return RefundInternalAsync(paymentTask, null, reason, idempotencyKey, cancellationToken);
    }

    public Task<Result<PaymentDetailDto>> PartialRefundAsync(
        int paymentId,
        decimal amount,
        string? reason,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var paymentTask = repository.GetByIdWithDetailsAsync(paymentId, cancellationToken);
        return RefundInternalAsync(paymentTask, amount, reason, idempotencyKey, cancellationToken);
    }

    private async Task<Result<PaymentDetailDto>> ExecuteProviderActionAsync(
        int paymentId,
        PaymentTransactionType transactionType,
        Func<IPaymentProvider, Payment, string, Task<PaymentResult>> action,
        CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdWithDetailsAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null || !IsStoreAccessible(payment.StoreId))
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.PaymentNotFound(paymentId));
        }

        var order = await orderRepository.GetByIdAsync(payment.OrderId, cancellationToken).ConfigureAwait(false);
        var methodSystemName = order?.SelectedPaymentMethodSystemName ?? PaymentProviderNames.FreeMethod;

        IPaymentProvider provider;
        try
        {
            provider = providerResolver.Resolve(payment.ProviderSystemName);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.ProviderNotFound(payment.ProviderSystemName));
        }

        PaymentResult providerResult;
        try
        {
            providerResult = await action(provider, payment, methodSystemName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            payment.AddTransaction(
                transactionType,
                payment.Amount,
                payment.Currency,
                PaymentTransactionStatus.Failed,
                failureMessage: ex.Message);
            await repository.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
            return Result.Failure<PaymentDetailDto>(PaymentErrors.InvalidPaymentState(ex.Message));
        }

        var attempt = payment.StartAttempt(payment.Attempts.Count + 1);
        ApplyProviderResult(payment, attempt, providerResult, transactionType);
        await repository.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
        await SyncOrderFromPaymentStatusAsync(payment, cancellationToken).ConfigureAwait(false);
        await PublishPaymentAuditAsync(payment, MapTransactionAction(transactionType), cancellationToken).ConfigureAwait(false);
        return Result.Success(PaymentMapper.ToDetail(payment));
    }

    private async Task<Result<PaymentDetailDto>> RefundInternalAsync(
        Task<Payment?> paymentTask,
        decimal? partialAmount,
        string? reason,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payment = await paymentTask.ConfigureAwait(false);
        if (payment is null || !IsStoreAccessible(payment.StoreId))
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.PaymentNotFound(0));
        }

        string? normalizedKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (normalizedKey is not null)
        {
            var existingRefund = await repository
                .GetRefundByIdempotencyKeyAsync(payment.Id, normalizedKey, cancellationToken)
                .ConfigureAwait(false);

            if (existingRefund is not null)
            {
                var reloaded = await repository.GetByIdWithDetailsAsync(payment.Id, cancellationToken).ConfigureAwait(false);
                return reloaded is null
                    ? Result.Failure<PaymentDetailDto>(PaymentErrors.PaymentNotFound(payment.Id))
                    : Result.Success(PaymentMapper.ToDetail(reloaded));
            }
        }

        var amount = partialAmount ?? (payment.Amount - payment.RefundedAmount);
        if (amount <= 0)
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.InvalidPaymentState("Refund amount must be greater than zero."));
        }

        IPaymentProvider provider;
        try
        {
            provider = providerResolver.Resolve(payment.ProviderSystemName);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.ProviderNotFound(payment.ProviderSystemName));
        }

        RefundResult providerResult;
        try
        {
            providerResult = await provider.RefundAsync(
                new RefundRequest(
                    payment.Id,
                    payment.StoreId,
                    amount,
                    payment.Currency,
                    reason,
                    normalizedKey,
                    payment.ProviderPaymentId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result.Failure<PaymentDetailDto>(PaymentErrors.InvalidPaymentState(ex.Message));
        }

        if (!providerResult.Success)
        {
            payment.AddTransaction(
                amount >= payment.Amount - payment.RefundedAmount
                    ? PaymentTransactionType.Refund
                    : PaymentTransactionType.PartialRefund,
                amount,
                payment.Currency,
                PaymentTransactionStatus.Failed,
                providerResult.ProviderTransactionId,
                failureCode: providerResult.FailureCode,
                failureMessage: providerResult.FailureMessage);
            await repository.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
            return Result.Failure<PaymentDetailDto>(
                PaymentErrors.InvalidPaymentState(providerResult.FailureMessage ?? "Refund failed."));
        }

        var refund = payment.ApplyRefund(amount, payment.Currency, reason, normalizedKey);
        refund.MarkSucceeded();
        refund.AddTransaction(
            amount,
            RefundStatus.Succeeded,
            providerTransactionId: providerResult.ProviderTransactionId);
        payment.AddTransaction(
            amount >= payment.Amount
                ? PaymentTransactionType.Refund
                : PaymentTransactionType.PartialRefund,
            amount,
            payment.Currency,
            PaymentTransactionStatus.Succeeded,
            providerResult.ProviderTransactionId);

        await repository.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
        await SyncOrderFromPaymentStatusAsync(payment, cancellationToken).ConfigureAwait(false);
        await PublishPaymentAuditAsync(payment, AuditActions.PaymentRefunded, cancellationToken).ConfigureAwait(false);
        return Result.Success(PaymentMapper.ToDetail(payment));
    }

    private async Task PublishPaymentAuditAsync(
        Payment payment,
        string action,
        CancellationToken cancellationToken) =>
        await auditPublisher.PublishAsync(new AuditPublishRequest(
            AuditCategory.Payment,
            action,
            Success: true,
            EntityType: nameof(Payment),
            EntityId: payment.Id.ToString(),
            StoreId: payment.StoreId,
            Details: new Dictionary<string, string?>
            {
                ["orderId"] = payment.OrderId.ToString(),
                ["provider"] = payment.ProviderSystemName,
                ["amount"] = payment.Amount.ToString("F2"),
                ["currency"] = payment.Currency
            }), cancellationToken).ConfigureAwait(false);

    private static string MapTransactionAction(PaymentTransactionType transactionType) =>
        transactionType switch
        {
            PaymentTransactionType.Capture => AuditActions.PaymentCaptured,
            PaymentTransactionType.Void => AuditActions.PaymentVoided,
            _ => AuditActions.PaymentCaptured
        };

    private async Task<PaymentMethod?> ResolveMethodAsync(
        int storeId,
        string? methodId,
        string methodSystemName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(methodId) && int.TryParse(methodId, out var id))
        {
            var byId = await repository.GetMethodByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (byId is not null && byId.StoreId == storeId && !byId.IsDeleted)
            {
                return byId;
            }
        }

        return await repository.GetMethodBySystemNameAsync(storeId, methodSystemName, cancellationToken).ConfigureAwait(false);
    }

    private static void ApplyProviderResult(
        Payment payment,
        PaymentAttempt attempt,
        PaymentResult providerResult,
        PaymentTransactionType transactionType)
    {
        if (providerResult.Success)
        {
            attempt.MarkSucceeded();
        }
        else
        {
            attempt.MarkFailed(providerResult.FailureMessage);
        }

        payment.AddTransaction(
            transactionType,
            payment.Amount,
            payment.Currency,
            providerResult.Success ? PaymentTransactionStatus.Succeeded : PaymentTransactionStatus.Failed,
            providerResult.ProviderPaymentId,
            failureCode: providerResult.FailureCode,
            failureMessage: providerResult.FailureMessage);

        if (!providerResult.Success)
        {
            payment.MarkFailed(providerResult.FailureMessage);
            return;
        }

        switch (providerResult.Status)
        {
            case PaymentStatus.Initiated:
                payment.MarkInitiated(providerResult.ProviderPaymentId);
                break;
            case PaymentStatus.RedirectRequired:
                payment.MarkRedirectRequired(providerResult.ProviderPaymentId);
                break;
            case PaymentStatus.Authorized:
                payment.MarkAuthorized(providerResult.ProviderPaymentId);
                break;
            case PaymentStatus.Captured:
                payment.MarkCaptured(providerResult.ProviderPaymentId);
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed(providerResult.FailureMessage);
                break;
            case PaymentStatus.Cancelled:
                payment.MarkCancelled(providerResult.FailureMessage);
                break;
            default:
                payment.MarkInitiated(providerResult.ProviderPaymentId);
                break;
        }
    }

    private static void ApplyVerificationResult(Payment payment, PaymentVerificationResult verification)
    {
        payment.AddTransaction(
            PaymentTransactionType.Verification,
            payment.Amount,
            payment.Currency,
            verification.Success ? PaymentTransactionStatus.Succeeded : PaymentTransactionStatus.Failed,
            verification.ProviderPaymentId,
            failureCode: verification.FailureCode,
            failureMessage: verification.FailureMessage);

        if (!verification.Success)
        {
            payment.MarkFailed(verification.FailureMessage);
            return;
        }

        switch (verification.Status)
        {
            case PaymentStatus.Authorized:
                payment.MarkAuthorized(verification.ProviderPaymentId);
                break;
            case PaymentStatus.Captured:
                payment.MarkCaptured(verification.ProviderPaymentId);
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed(verification.FailureMessage);
                break;
            default:
                payment.MarkCaptured(verification.ProviderPaymentId);
                break;
        }
    }

    private async Task SyncOrderFromPaymentStatusAsync(Payment payment, CancellationToken cancellationToken)
    {
        switch (payment.Status)
        {
            case PaymentStatus.Authorized:
                await orderPaymentSyncService.SyncAuthorizedAsync(payment.OrderId, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case PaymentStatus.Captured:
                await orderPaymentSyncService.SyncPaidAsync(payment.OrderId, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case PaymentStatus.Failed:
            case PaymentStatus.Cancelled:
                await orderPaymentSyncService.SyncFailedAsync(payment.OrderId, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case PaymentStatus.PartiallyRefunded:
                await orderPaymentSyncService.SyncPartialRefundAsync(payment.OrderId, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case PaymentStatus.Refunded:
                await orderPaymentSyncService.SyncFullRefundAsync(payment.OrderId, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private bool IsStoreAccessible(int storeId) =>
        storeContext.CurrentStoreId.HasValue && storeContext.CurrentStoreId.Value == storeId;
}
