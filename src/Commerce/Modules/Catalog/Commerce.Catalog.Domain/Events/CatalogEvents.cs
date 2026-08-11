namespace Commerce.Catalog.Domain.Events;

public sealed record ProductCreatedEvent(int ProductId, string Sku, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record ProductUpdatedEvent(int ProductId, string Sku, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record ProductDeletedEvent(int ProductId, string Sku, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CategoryCreatedEvent(int CategoryId, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CategoryUpdatedEvent(int CategoryId, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CategoryDeletedEvent(int CategoryId, string Name) : Commerce.Framework.Core.Events.DomainEvent;
