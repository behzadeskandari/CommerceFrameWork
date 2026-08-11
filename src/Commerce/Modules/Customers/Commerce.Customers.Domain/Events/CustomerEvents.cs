namespace Commerce.Customers.Domain.Events;

public sealed record CustomerRegisteredEvent(int CustomerId, string Email) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CustomerUpdatedEvent(int CustomerId, string Email) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CustomerDeactivatedEvent(int CustomerId, string Email) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CustomerAddressAddedEvent(int CustomerId, int AddressId) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CustomerAddressRemovedEvent(int CustomerId, int AddressId) : Commerce.Framework.Core.Events.DomainEvent;
