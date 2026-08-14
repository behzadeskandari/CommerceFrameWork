namespace Commerce.Framework.Events;

public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;

    public abstract string EventType { get; }

    public int? StoreId { get; init; }
}
