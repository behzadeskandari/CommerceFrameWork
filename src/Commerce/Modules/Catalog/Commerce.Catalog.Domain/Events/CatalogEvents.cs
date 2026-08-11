namespace Commerce.Catalog.Domain.Events;

public sealed record ProductCreatedEvent(int ProductId, string Sku, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record ProductUpdatedEvent(int ProductId, string Sku, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record ProductDeletedEvent(int ProductId, string Sku, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CategoryCreatedEvent(int CategoryId, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CategoryUpdatedEvent(int CategoryId, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record CategoryDeletedEvent(int CategoryId, string Name) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record VariantCreatedEvent(int VariantId, int ProductId, string Sku) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record VariantUpdatedEvent(int VariantId, int ProductId, string Sku) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record OfferCreatedEvent(int OfferId, int ProductId, int? VariantId, int StoreId) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record OfferUpdatedEvent(int OfferId, int ProductId, int? VariantId, int StoreId) : Commerce.Framework.Core.Events.DomainEvent;
