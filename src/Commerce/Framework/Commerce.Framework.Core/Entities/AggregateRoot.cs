using Commerce.Framework.Core.Events;

namespace Commerce.Framework.Core.Entities;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
}

public abstract class AggregateRoot : AggregateRoot<int>;
