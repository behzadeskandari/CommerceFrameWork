using Commerce.Framework.Core.Errors;

namespace Commerce.Cart.Application.Carts;

internal static class CartErrors
{
    internal static Error CartNotFound() =>
        Error.NotFound("Cart was not found.");

    internal static Error StoreContextRequired() =>
        Error.Validation("Store context is required.");

    internal static Error CurrencyContextRequired() =>
        Error.Validation("Currency context is required.");

    internal static Error OfferNotFound(int offerId) =>
        Error.NotFound($"Offer '{offerId}' was not found.");

    internal static Error OfferUnavailable(string detail) =>
        Error.Validation("Offer is unavailable.", detail);

    internal static Error InvalidQuantity(string detail) =>
        Error.Validation("Invalid quantity.", detail);

    internal static Error CurrencyMismatch() =>
        Error.Validation("Offer currency does not match cart currency.");

    internal static Error StoreMismatch() =>
        Error.Validation("Offer belongs to a different store.");

    internal static Error CartExpired() =>
        Error.Validation("Cart has expired.");

    internal static Error CartConverted() =>
        Error.Validation("Cart has already been converted.");

    internal static Error UnauthorizedCartAccess() =>
        Error.Validation("You are not authorized to access this cart.");

    internal static Error CartItemNotFound(int cartItemId) =>
        Error.NotFound($"Cart item '{cartItemId}' was not found.");

    internal static Error CustomerRequired() =>
        Error.Validation("Customer authentication is required.");

    internal static Error GuestCartNotFound() =>
        Error.NotFound("Guest cart was not found.");
}
