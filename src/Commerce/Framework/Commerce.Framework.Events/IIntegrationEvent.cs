namespace Commerce.Framework.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }

    string EventType { get; }

    int? StoreId { get; }
}
