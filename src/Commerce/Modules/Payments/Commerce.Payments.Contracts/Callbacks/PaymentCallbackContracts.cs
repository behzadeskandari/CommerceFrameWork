using Commerce.Framework.Core.Results;
using Commerce.Payments.Contracts.Payments;

namespace Commerce.Payments.Contracts.Callbacks;



public sealed record PaymentCallbackContext(
    string ProviderSystemName,
    string CallbackKey,
    string PayloadHash,
    IReadOnlyDictionary<string, string> Data,
    IReadOnlyDictionary<string, string>? Headers = null);

public interface IPaymentCallbackHandler
{
    string ProviderSystemName { get; }

    Task<Result<int?>> HandleAsync(PaymentCallbackContext context, CancellationToken cancellationToken = default);
}

public interface IPaymentCallbackDispatcher
{
    Task<Result<PaymentCallbackDispatchResult>> DispatchAsync(
        PaymentCallbackContext context,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentCallbackDispatchResult(
    bool Ignored,
    PaymentDetailDto? Payment);

