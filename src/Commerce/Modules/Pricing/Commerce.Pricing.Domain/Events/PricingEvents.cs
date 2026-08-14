using Commerce.Framework.Core.Events;

namespace Commerce.Pricing.Domain.Events;

public sealed record DiscountCreatedEvent(int DiscountId, string Name) : DomainEvent;

public sealed record DiscountUpdatedEvent(int DiscountId, string Name) : DomainEvent;

public sealed record DiscountActivatedEvent(int DiscountId) : DomainEvent;

public sealed record DiscountDeactivatedEvent(int DiscountId) : DomainEvent;

public sealed record CouponCreatedEvent(int CouponId, string Code) : DomainEvent;

public sealed record CouponUsedEvent(int CouponId, string Code, int OrderId, int? CustomerId) : DomainEvent;

public sealed record CouponUsageReleasedEvent(int CouponId, int OrderId) : DomainEvent;
