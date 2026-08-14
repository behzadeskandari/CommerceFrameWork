using Commerce.Framework.Events;

namespace Commerce.Integration.Contracts.Events;

public static class IntegrationEventTypes
{
    public const string OrderCreated = "OrderCreated";
    public const string OrderPaid = "OrderPaid";
    public const string OrderCancelled = "OrderCancelled";
    public const string PaymentSucceeded = "PaymentSucceeded";
    public const string PaymentFailed = "PaymentFailed";
    public const string ProductCreated = "ProductCreated";
    public const string ProductUpdated = "ProductUpdated";
    public const string CustomerRegistered = "CustomerRegistered";
    public const string InventoryChanged = "InventoryChanged";
    public const string ShipmentCreated = "ShipmentCreated";
    public const string RefundCreated = "RefundCreated";
}

public sealed record OrderCreatedIntegrationEvent(
    int OrderId,
    string OrderNumber,
    int? CustomerId,
    decimal GrandTotal,
    string CurrencyCode) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.OrderCreated;
}

public sealed record OrderPaidIntegrationEvent(
    int OrderId,
    string OrderNumber,
    int? CustomerId,
    decimal GrandTotal,
    string CurrencyCode) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.OrderPaid;
}

public sealed record OrderCancelledIntegrationEvent(
    int OrderId,
    string OrderNumber,
    string Reason) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.OrderCancelled;
}

public sealed record PaymentSucceededIntegrationEvent(
    int OrderId,
    int? PaymentId,
    decimal Amount,
    string CurrencyCode,
    string? PaymentMethodSystemName) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.PaymentSucceeded;
}

public sealed record PaymentFailedIntegrationEvent(
    int OrderId,
    int? PaymentId,
    string? Reason) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.PaymentFailed;
}

public sealed record ProductCreatedIntegrationEvent(
    int ProductId,
    string Sku,
    string Name) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.ProductCreated;
}

public sealed record ProductUpdatedIntegrationEvent(
    int ProductId,
    string Sku,
    string Name) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.ProductUpdated;
}

public sealed record CustomerRegisteredIntegrationEvent(
    int CustomerId,
    string Email) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.CustomerRegistered;
}

public sealed record InventoryChangedIntegrationEvent(
    int InventoryItemId,
    int OfferId,
    int QuantityDelta,
    string MovementType,
    int AvailableQuantity) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.InventoryChanged;
}

public sealed record ShipmentCreatedIntegrationEvent(
    int OrderId,
    string OrderNumber,
    string? TrackingNumber) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.ShipmentCreated;
}

public sealed record RefundCreatedIntegrationEvent(
    int OrderId,
    string OrderNumber,
    bool IsFullRefund,
    string? Reason) : IntegrationEventBase
{
    public override string EventType => IntegrationEventTypes.RefundCreated;
}

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventIdempotencyService
{
    Task<bool> TryMarkProcessedAsync(
        Guid integrationEventId,
        string eventType,
        string consumerKey,
        CancellationToken cancellationToken = default);
}
