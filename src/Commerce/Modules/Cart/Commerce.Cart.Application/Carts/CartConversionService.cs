using Commerce.Cart.Application.Abstractions;
using Commerce.Cart.Contracts.Carts;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Microsoft.Extensions.Logging;

namespace Commerce.Cart.Application.Carts;

public sealed class CartConversionService(
    ICartRepository cartRepository,
    ILogger<CartConversionService> logger) : ICartConversionService
{
    public async Task<Result> MarkConvertedAsync(int cartId, CancellationToken cancellationToken = default)
    {
        var cart = await cartRepository.GetByIdWithItemsAsync(cartId, cancellationToken).ConfigureAwait(false);
        if (cart is null)
        {
            return Result.Failure(Error.NotFound($"Cart '{cartId}' was not found."));
        }

        if (cart.Status == Commerce.Cart.Domain.Enums.CartStatus.Converted)
        {
            return Result.Success();
        }

        try
        {
            cart.MarkConverted();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict(ex.Message));
        }

        await cartRepository.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Cart {CartId} marked converted.", cartId);
        return Result.Success();
    }
}
