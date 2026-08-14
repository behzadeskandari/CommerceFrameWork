using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class StoreCreditAccount : AggregateRoot
{
    public const int CurrencyCodeMaxLength = 8;
    public const int IdempotencyKeyMaxLength = 128;

    private readonly List<StoreCreditTransaction> _transactions = [];

    private StoreCreditAccount()
    {
    }

    public int CustomerId { get; private set; }

    public int StoreId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal Balance { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<StoreCreditTransaction> Transactions => _transactions;

    public static StoreCreditAccount Create(int customerId, int storeId, string currencyCode)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }

        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        var utcNow = DateTime.UtcNow;
        return new StoreCreditAccount
        {
            CustomerId = customerId,
            StoreId = storeId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Balance = 0m,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public StoreCreditTransaction PostTransaction(
        StoreCreditTransactionType type,
        decimal amountDelta,
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

        if (amountDelta == 0m && type is not StoreCreditTransactionType.Adjust)
        {
            throw new ArgumentOutOfRangeException(nameof(amountDelta));
        }

        var newBalance = Balance + amountDelta;
        if (newBalance < 0m)
        {
            throw new InvalidOperationException("Insufficient store credit balance.");
        }

        var transaction = StoreCreditTransaction.Create(
            Id,
            type,
            amountDelta,
            newBalance,
            CurrencyCode,
            normalizedKey,
            referenceType,
            referenceId,
            reason,
            expiresAtUtc);

        Balance = newBalance;
        _transactions.Add(transaction);
        UpdatedAtUtc = DateTime.UtcNow;
        return transaction;
    }

    public decimal GetExpirableBalance(DateTime utcNow) =>
        _transactions
            .Where(x =>
                x.Type == StoreCreditTransactionType.Credit &&
                x.ExpiresAtUtc.HasValue &&
                x.ExpiresAtUtc.Value <= utcNow &&
                !x.IsExpired)
            .Sum(x => x.AmountDelta);
}

public sealed class StoreCreditTransaction : Entity
{
    public const int ReasonMaxLength = 500;

    private StoreCreditTransaction()
    {
    }

    public int StoreCreditAccountId { get; private set; }

    public StoreCreditTransactionType Type { get; private set; }

    public decimal AmountDelta { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public CustomerAccountReferenceType ReferenceType { get; private set; }

    public int? ReferenceId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime? ExpiresAtUtc { get; private set; }

    public bool IsExpired { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static StoreCreditTransaction Create(
        int storeCreditAccountId,
        StoreCreditTransactionType type,
        decimal amountDelta,
        decimal balanceAfter,
        string currencyCode,
        string idempotencyKey,
        CustomerAccountReferenceType referenceType,
        int? referenceId,
        string? reason,
        DateTime? expiresAtUtc)
    {
        return new StoreCreditTransaction
        {
            StoreCreditAccountId = storeCreditAccountId,
            Type = type,
            AmountDelta = amountDelta,
            BalanceAfter = balanceAfter,
            CurrencyCode = currencyCode,
            IdempotencyKey = idempotencyKey,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ExpiresAtUtc = expiresAtUtc,
            IsExpired = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkExpired() => IsExpired = true;
}
