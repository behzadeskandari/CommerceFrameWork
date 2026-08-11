namespace Commerce.Framework.Core.Events;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid EventId { get; init; }

    public DateTime OccurredOnUtc { get; init; }
}
