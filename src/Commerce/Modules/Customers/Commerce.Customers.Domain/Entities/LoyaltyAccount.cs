using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class LoyaltyAccount : AggregateRoot
{
    public const int IdempotencyKeyMaxLength = 128;

    private readonly List<LoyaltyTransaction> _transactions = [];

    private LoyaltyAccount()
    {
    }

    public int CustomerId { get; private set; }

    public int StoreId { get; private set; }

    public int PointsBalance { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<LoyaltyTransaction> Transactions => _transactions;

    public static LoyaltyAccount Create(int customerId, int storeId)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }

        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        var utcNow = DateTime.UtcNow;
        return new LoyaltyAccount
        {
            CustomerId = customerId,
            StoreId = storeId,
            PointsBalance = 0,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public LoyaltyTransaction PostTransaction(
        LoyaltyTransactionType type,
        int pointsDelta,
        string idempotencyKey,
        CustomerAccountReferenceType referenceType,
        int? referenceId,
        string? reason,
        DateTime? expiresAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        var normalizedKey = idempotencyKey.Trim();
        var existing = _transactions.FirstOrDefault(x =>
            string.Equals(x.IdempotencyKey, normalizedKey, StringComparison.Ordinal));

        if (existing is not null)
        {
            return existing;
        }

        if (pointsDelta == 0 && type is not LoyaltyTransactionType.Adjust)
        {
            throw new ArgumentOutOfRangeException(nameof(pointsDelta));
        }

        var newBalance = PointsBalance + pointsDelta;
        if (newBalance < 0)
        {
            throw new InvalidOperationException("Insufficient loyalty points.");
        }

        var transaction = LoyaltyTransaction.Create(
            Id,
            type,
            pointsDelta,
            newBalance,
            normalizedKey,
            referenceType,
            referenceId,
            reason,
            expiresAtUtc);

        PointsBalance = newBalance;
        _transactions.Add(transaction);
        UpdatedAtUtc = DateTime.UtcNow;
        return transaction;
    }

    public int GetExpirablePoints(DateTime utcNow) =>
        _transactions
            .Where(x =>
                x.Type == LoyaltyTransactionType.Earn &&
                x.ExpiresAtUtc.HasValue &&
                x.ExpiresAtUtc.Value <= utcNow &&
                !x.IsExpired)
            .Sum(x => x.PointsDelta);
}

public sealed class LoyaltyTransaction : Entity
{
    public const int ReasonMaxLength = 500;

    private LoyaltyTransaction()
    {
    }

    public int LoyaltyAccountId { get; private set; }

    public LoyaltyTransactionType Type { get; private set; }

    public int PointsDelta { get; private set; }

    public int BalanceAfter { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public CustomerAccountReferenceType ReferenceType { get; private set; }

    public int? ReferenceId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime? ExpiresAtUtc { get; private set; }

    public bool IsExpired { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static LoyaltyTransaction Create(
        int loyaltyAccountId,
        LoyaltyTransactionType type,
        int pointsDelta,
        int balanceAfter,
        string idempotencyKey,
        CustomerAccountReferenceType referenceType,
        int? referenceId,
        string? reason,
        DateTime? expiresAtUtc)
    {
        return new LoyaltyTransaction
        {
            LoyaltyAccountId = loyaltyAccountId,
            Type = type,
            PointsDelta = pointsDelta,
            BalanceAfter = balanceAfter,
            IdempotencyKey = idempotencyKey,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ExpiresAtUtc = expiresAtUtc,
            IsExpired = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkExpired()
    {
        IsExpired = true;
    }
}
