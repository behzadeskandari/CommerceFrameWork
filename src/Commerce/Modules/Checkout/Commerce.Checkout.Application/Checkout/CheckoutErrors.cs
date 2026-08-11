using Commerce.Framework.Core.Errors;

namespace Commerce.Checkout.Application.Checkout;

internal static class CheckoutErrors
{
    internal static Error CheckoutNotFound() => Error.NotFound("Checkout session was not found.");

    internal static Error CheckoutCartEmpty() => Error.Validation("Checkout cannot start with an empty cart.");

    internal static Error StoreContextRequired() => Error.Validation("Store context is required.");

    internal static Error CurrencyContextRequired() => Error.Validation("Currency context is required.");

    internal static Error UnauthorizedCheckoutAccess() => Error.Validation("You are not authorized to access this checkout.");

    internal static Error CheckoutExpired() => Error.Validation("Checkout session has expired.");

    internal static Error CartInvalid(string detail) => Error.Validation("Cart is not valid for checkout.", detail);

    internal static Error AddressNotFound() => Error.NotFound("Address was not found.");

    internal static Error ShippingMethodNotFound() => Error.NotFound("Shipping method was not found.");

    internal static Error PaymentMethodNotFound() => Error.NotFound("Payment method was not found.");

    internal static Error GuestEmailRequired() => Error.Validation("Guest email is required.");

    internal static Error BillingAddressRequired() => Error.Validation("Billing address is required.");

    internal static Error ShippingAddressRequired() => Error.Validation("Shipping address is required.");

    internal static Error ShippingMethodRequired() => Error.Validation("Shipping method is required.");

    internal static Error PaymentMethodRequired() => Error.Validation("Payment method is required.");
}
