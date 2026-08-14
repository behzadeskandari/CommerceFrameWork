using Commerce.Framework.Core.Errors;



namespace Commerce.Payments.Application.Payments;



internal static class PaymentErrors

{

    internal static Error PaymentNotFound(int paymentId) =>

        Error.NotFound($"Payment '{paymentId}' was not found.");



    internal static Error PaymentNotFoundForOrder(int orderId) =>

        Error.NotFound($"Payment for order '{orderId}' was not found.");



    internal static Error OrderPaymentAlreadyExists(int orderId) =>

        Error.Conflict($"A payment already exists for order '{orderId}'.");



    internal static Error PaymentMethodNotFound() =>

        Error.NotFound("Payment method was not found.");



    internal static Error ProviderNotFound(string providerSystemName) =>

        Error.NotFound($"Payment provider '{providerSystemName}' was not found.");



    internal static Error StoreMismatch() =>

        Error.Validation("Store context is required.");



    internal static Error IdempotencyKeyRequired() =>

        Error.Validation("Idempotency-Key header is required.");



    internal static Error InvalidPaymentState(string message) =>

        Error.Validation(message);



    internal static Error OrderNotFound(int orderId) =>

        Error.NotFound($"Order '{orderId}' was not found.");



    internal static Error CallbackAlreadyProcessed() =>

        Error.Conflict("Callback has already been processed.");

}

