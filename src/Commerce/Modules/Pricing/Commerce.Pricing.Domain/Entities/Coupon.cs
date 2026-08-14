using Commerce.Pricing.Domain.Enums;
using Commerce.Pricing.Domain.Events;
using Commerce.Framework.Core.Entities;

namespace Commerce.Pricing.Domain.Entities;

public sealed class Coupon : AggregateRoot
{
    public const int CodeMaxLength = 64;

    private Coupon()
    {
    }

    public int DiscountId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime? StartsAtUtc { get; private set; }

    public DateTime? EndsAtUtc { get; private set; }

    public int? StoreId { get; private set; }

    public int? GlobalUsageLimit { get; private set; }

    public int? PerCustomerUsageLimit { get; private set; }

    public int UsageCount { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();

    public static Coupon Create(
        int discountId,
        string code,
        bool isActive,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? storeId,
        int? globalUsageLimit,
        int? perCustomerUsageLimit)
    {
        if (discountId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountId));
        }

        ValidateCode(code);
        ValidateDateRange(startsAtUtc, endsAtUtc);
        ValidateUsageLimits(globalUsageLimit, perCustomerUsageLimit);

        var utcNow = DateTime.UtcNow;
        var normalized = NormalizeCode(code);
        var coupon = new Coupon
        {
            DiscountId = discountId,
            Code = normalized,
            IsActive = isActive,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            StoreId = storeId,
            GlobalUsageLimit = globalUsageLimit,
            PerCustomerUsageLimit = perCustomerUsageLimit,
            UsageCount = 0,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        coupon.RaiseDomainEvent(new CouponCreatedEvent(0, normalized));
        return coupon;
    }

    public void Update(
        bool isActive,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? storeId,
        int? globalUsageLimit,
        int? perCustomerUsageLimit)
    {
        EnsureNotDeleted();
        ValidateDateRange(startsAtUtc, endsAtUtc);
        ValidateUsageLimits(globalUsageLimit, perCustomerUsageLimit);

        IsActive = isActive;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        StoreId = storeId;
        GlobalUsageLimit = globalUsageLimit;
        PerCustomerUsageLimit = perCustomerUsageLimit;
        Touch();
    }

    public void SoftDelete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public bool IsCurrentlyValid(DateTime utcNow) =>
        !IsDeleted &&
        IsActive &&
        (!StartsAtUtc.HasValue || utcNow >= StartsAtUtc.Value) &&
        (!EndsAtUtc.HasValue || utcNow <= EndsAtUtc.Value);

    public bool AppliesToStore(int storeId) => !StoreId.HasValue || StoreId.Value == storeId;

    public bool HasGlobalUsageRemaining() =>
        !GlobalUsageLimit.HasValue || UsageCount < GlobalUsageLimit.Value;

    public void RecordUsage(int orderId, int? customerId)
    {
        EnsureNotDeleted();
        if (!HasGlobalUsageRemaining())
        {
            throw new InvalidOperationException("Coupon global usage limit reached.");
        }

        UsageCount++;
        Touch();
        RaiseDomainEvent(new CouponUsedEvent(Id, Code, orderId, customerId));
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Coupon has been deleted.");
        }
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Coupon code is required.", nameof(code));
        }

        if (code.Trim().Length > CodeMaxLength)
        {
            throw new ArgumentException($"Coupon code cannot exceed {CodeMaxLength} characters.", nameof(code));
        }
    }

    private static void ValidateDateRange(DateTime? startsAtUtc, DateTime? endsAtUtc)
    {
        if (startsAtUtc.HasValue && endsAtUtc.HasValue && startsAtUtc.Value > endsAtUtc.Value)
        {
            throw new ArgumentException("Start date must be before end date.");
        }
    }

    private static void ValidateUsageLimits(int? globalUsageLimit, int? perCustomerUsageLimit)
    {
        if (globalUsageLimit.HasValue && globalUsageLimit.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(globalUsageLimit));
        }

        if (perCustomerUsageLimit.HasValue && perCustomerUsageLimit.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(perCustomerUsageLimit));
        }
    }
}
