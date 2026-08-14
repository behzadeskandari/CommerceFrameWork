using Commerce.Framework.Core.Results;
using Commerce.Payments.Contracts.Callbacks;
using Commerce.Payments.Contracts.Payments;

namespace Commerce.Payments.Application.Payments;

public sealed class PaymentCallbackDispatcher(
    IPaymentService paymentService,
    IEnumerable<IPaymentCallbackHandler> callbackHandlers) : IPaymentCallbackDispatcher
{
    public async Task<Result<PaymentCallbackDispatchResult>> DispatchAsync(
        PaymentCallbackContext context,
        CancellationToken cancellationToken = default)
    {
        var handler = callbackHandlers.FirstOrDefault(x =>
            string.Equals(x.ProviderSystemName, context.ProviderSystemName, StringComparison.OrdinalIgnoreCase));

        if (handler is not null)
        {
            var handlerResult = await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
            if (!handlerResult.IsSuccess)
            {
                return Result.Failure<PaymentCallbackDispatchResult>(handlerResult.Error!);
            }

            if (handlerResult.Value is null)
            {
                return Result.Success(new PaymentCallbackDispatchResult(true, null));
            }

            var payment = await paymentService
                .GetByIdAsync(handlerResult.Value.Value, cancellationToken)
                .ConfigureAwait(false);

            return payment.IsSuccess
                ? Result.Success(new PaymentCallbackDispatchResult(false, payment.Value))
                : Result.Failure<PaymentCallbackDispatchResult>(payment.Error!);
        }

        var result = await paymentService.ProcessCallbackAsync(
            context.ProviderSystemName,
            context.CallbackKey,
            context.PayloadHash,
            context.Data,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Result.Success(new PaymentCallbackDispatchResult(false, result.Value))
            : Result.Failure<PaymentCallbackDispatchResult>(result.Error!);
    }
}
