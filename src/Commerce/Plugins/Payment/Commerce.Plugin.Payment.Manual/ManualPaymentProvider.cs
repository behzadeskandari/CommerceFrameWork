using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Domain.Enums;

namespace Commerce.Plugin.Payment.Manual;

public sealed class ManualPaymentProvider : IPaymentProvider
{
    public string ProviderSystemName => PaymentProviderNames.Manual;

    public Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(request.PaymentMethodSystemName, PaymentProviderNames.FreeMethod, StringComparison.OrdinalIgnoreCase))
        {
            if (request.Amount == 0)
            {
                return Task.FromResult(new PaymentResult(
                    Success: true,
                    Status: PaymentStatus.Captured,
                    ProviderPaymentId: $"manual-free-{request.PaymentId}",
                    Instructions: "Free order — payment captured automatically."));
            }

            return Task.FromResult(new PaymentResult(
                Success: false,
                Status: PaymentStatus.Failed,
                FailureCode: "invalid_amount",
                FailureMessage: "Free payment method requires zero amount."));
        }

        if (string.Equals(request.PaymentMethodSystemName, "bank-transfer", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PaymentResult(
                Success: true,
                Status: PaymentStatus.Initiated,
                ProviderPaymentId: $"manual-bank-{request.PaymentId}",
                Instructions: "Please transfer the order total to the store bank account. Reference your order number."));
        }

        if (string.Equals(request.PaymentMethodSystemName, "cash-on-delivery", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PaymentResult(
                Success: true,
                Status: PaymentStatus.Authorized,
                ProviderPaymentId: $"manual-cod-{request.PaymentId}",
                Instructions: "Pay with cash when your order is delivered."));
        }

        return Task.FromResult(new PaymentResult(
            Success: true,
            Status: PaymentStatus.Initiated,
            ProviderPaymentId: $"manual-{request.PaymentId}",
            Instructions: "Complete payment using the selected manual method."));
    }

    public Task<PaymentVerificationResult> GetPaymentStatusAsync(
        int paymentId,
        string? providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var isFree = providerPaymentId?.Contains("free", StringComparison.OrdinalIgnoreCase) == true;
        return Task.FromResult(new PaymentVerificationResult(
            Success: true,
            Status: isFree ? PaymentStatus.Captured : PaymentStatus.Initiated,
            ProviderPaymentId: providerPaymentId));
    }

    public Task<PaymentVerificationResult> VerifyPaymentAsync(
        int paymentId,
        string? providerPaymentId,
        IReadOnlyDictionary<string, string>? callbackData = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (providerPaymentId?.Contains("cod", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Task.FromResult(new PaymentVerificationResult(
                Success: true,
                Status: PaymentStatus.Authorized,
                ProviderPaymentId: providerPaymentId));
        }

        if (providerPaymentId?.Contains("free", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Task.FromResult(new PaymentVerificationResult(
                Success: true,
                Status: PaymentStatus.Captured,
                ProviderPaymentId: providerPaymentId));
        }

        var confirmed = callbackData is not null &&
                        callbackData.TryGetValue("confirmed", out var value) &&
                        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(new PaymentVerificationResult(
            Success: confirmed,
            Status: confirmed ? PaymentStatus.Captured : PaymentStatus.Initiated,
            ProviderPaymentId: providerPaymentId,
            FailureMessage: confirmed ? null : "Manual payment not yet confirmed."));
    }

    public Task<PaymentResult> CaptureAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentResult(
            Success: true,
            Status: PaymentStatus.Captured,
            ProviderPaymentId: request.PaymentMethodSystemName));
    }

    public Task<PaymentResult> VoidAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentResult(
            Success: true,
            Status: PaymentStatus.Cancelled,
            ProviderPaymentId: request.PaymentMethodSystemName));
    }

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new RefundResult(
            Success: true,
            Status: RefundStatus.Succeeded,
            ProviderTransactionId: $"manual-refund-{request.PaymentId}-{request.Amount:F2}"));
    }
}
