using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class LoyaltyReward : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    private LoyaltyReward()
    {
    }

    public int StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int PointsCost { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static LoyaltyReward Create(
        int storeId,
        string name,
        int pointsCost,
        string? description = null)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Reward name is required.", nameof(name));
        }

        if (pointsCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointsCost));
        }

        var utcNow = DateTime.UtcNow;
        return new LoyaltyReward
        {
            StoreId = storeId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            PointsCost = pointsCost,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(string name, int pointsCost, string? description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Reward name is required.", nameof(name));
        }

        if (pointsCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointsCost));
        }

        Name = name.Trim();
        PointsCost = pointsCost;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class LoyaltyRewardRedemption : AggregateRoot
{
    public const int IdempotencyKeyMaxLength = 128;

    private LoyaltyRewardRedemption()
    {
    }

    public int CustomerId { get; private set; }

    public int StoreId { get; private set; }

    public int LoyaltyRewardId { get; private set; }

    public int PointsSpent { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public LoyaltyRewardRedemptionStatus Status { get; private set; }

    public int? LoyaltyTransactionId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static LoyaltyRewardRedemption Create(
        int customerId,
        int storeId,
        int loyaltyRewardId,
        int pointsSpent,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        var utcNow = DateTime.UtcNow;
        return new LoyaltyRewardRedemption
        {
            CustomerId = customerId,
            StoreId = storeId,
            LoyaltyRewardId = loyaltyRewardId,
            PointsSpent = pointsSpent,
            IdempotencyKey = idempotencyKey.Trim(),
            Status = LoyaltyRewardRedemptionStatus.Pending,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Complete(int loyaltyTransactionId)
    {
        Status = LoyaltyRewardRedemptionStatus.Completed;
        LoyaltyTransactionId = loyaltyTransactionId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = LoyaltyRewardRedemptionStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
