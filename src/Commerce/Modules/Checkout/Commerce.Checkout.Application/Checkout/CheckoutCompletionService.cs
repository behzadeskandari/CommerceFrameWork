using Commerce.Checkout.Application.Abstractions;
using Commerce.Checkout.Contracts.Checkout;
using Commerce.Checkout.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Microsoft.Extensions.Logging;

namespace Commerce.Checkout.Application.Checkout;

public sealed class CheckoutCompletionService(
    ICheckoutRepository checkoutRepository,
    ILogger<CheckoutCompletionService> logger) : ICheckoutCompletionService
{
    public async Task<Result> MarkCompletedAsync(int checkoutId, CancellationToken cancellationToken = default)
    {
        var session = await checkoutRepository.GetByIdWithItemsAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return Result.Failure(Error.NotFound($"Checkout '{checkoutId}' was not found."));
        }

        if (session.Status == CheckoutStatus.Completed)
        {
            return Result.Success();
        }

        try
        {
            session.MarkCompleted();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict(ex.Message));
        }

        await checkoutRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Checkout {CheckoutId} marked completed.", checkoutId);
        return Result.Success();
    }
}
