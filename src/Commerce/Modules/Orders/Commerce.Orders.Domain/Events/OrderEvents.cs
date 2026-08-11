namespace Commerce.Orders.Domain.Events;

public sealed record OrderCreatedEvent(
    string OrderNumber,
    int StoreId,
    int? CustomerId) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record OrderCancelledEvent(
    int OrderId,
    string OrderNumber,
    string Reason) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record OrderStatusChangedEvent(
    int OrderId,
    string FromStatus,
    string ToStatus) : Commerce.Framework.Core.Events.DomainEvent;
