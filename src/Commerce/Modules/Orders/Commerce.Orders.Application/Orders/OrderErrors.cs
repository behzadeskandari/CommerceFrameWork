using Commerce.Framework.Core.Errors;

namespace Commerce.Orders.Application.Orders;

public static class OrderErrors
{
    public static Error CheckoutNotFound(int checkoutId) =>
        Error.NotFound($"Checkout '{checkoutId}' was not found.");

    public static Error CheckoutNotReady(string message) =>
        Error.Validation(message);

    public static Error CheckoutExpired() =>
        Error.Validation("Checkout has expired.");

    public static Error CheckoutOwnershipViolation() =>
        Error.Validation("Checkout does not belong to the current customer or guest.");

    public static Error CheckoutAlreadyConverted() =>
        Error.Conflict("Checkout has already been converted to an order.");

    public static Error OrderCreationConflict(string message) =>
        Error.Conflict(message);

    public static Error OrderAlreadyCreated(int orderId) =>
        Error.Conflict($"An order already exists for this checkout (order '{orderId}').");

    public static Error InvalidOrderState(string message) =>
        Error.Validation(message);

    public static Error OrderNotFound(int orderId) =>
        Error.NotFound($"Order '{orderId}' was not found.");

    public static Error OrderNotFoundByNumber(string orderNumber) =>
        Error.NotFound($"Order '{orderNumber}' was not found.");

    public static Error OrderAccessDenied() =>
        Error.Validation("Access to this order is denied.");

    public static Error StoreMismatch() =>
        Error.Validation("Checkout does not belong to the current store.");

    public static Error IdempotencyKeyRequired() =>
        Error.Validation("Idempotency-Key header is required for order creation.");
}
