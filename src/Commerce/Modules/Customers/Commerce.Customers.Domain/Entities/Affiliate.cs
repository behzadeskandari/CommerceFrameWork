using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class Affiliate : AggregateRoot
{
    public const int ReferralCodeMaxLength = 64;

    private Affiliate()
    {
    }

    public int CustomerId { get; private set; }

    public int StoreId { get; private set; }

    public string ReferralCode { get; private set; } = string.Empty;

    public decimal CommissionRatePercent { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static string NormalizeReferralCode(string code) => code.Trim().ToUpperInvariant();

    public static Affiliate Create(
        int customerId,
        int storeId,
        string referralCode,
        decimal commissionRatePercent,
        bool isActive)
    {
        ValidateCustomer(customerId);
        ValidateStore(storeId);
        ValidateReferralCode(referralCode);
        ValidateCommissionRate(commissionRatePercent);

        var utcNow = DateTime.UtcNow;
        return new Affiliate
        {
            CustomerId = customerId,
            StoreId = storeId,
            ReferralCode = NormalizeReferralCode(referralCode),
            CommissionRatePercent = commissionRatePercent,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(decimal commissionRatePercent, bool isActive)
    {
        EnsureNotDeleted();
        ValidateCommissionRate(commissionRatePercent);
        CommissionRatePercent = commissionRatePercent;
        IsActive = isActive;
        Touch();
    }

    public void SoftDelete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public bool IsCurrentlyActive() => !IsDeleted && IsActive;

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Affiliate has been deleted.");
        }
    }

    private static void ValidateCustomer(int customerId)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }
    }

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }
    }

    private static void ValidateReferralCode(string referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
        {
            throw new ArgumentException("Referral code is required.", nameof(referralCode));
        }

        if (referralCode.Trim().Length > ReferralCodeMaxLength)
        {
            throw new ArgumentException($"Referral code cannot exceed {ReferralCodeMaxLength} characters.", nameof(referralCode));
        }
    }

    private static void ValidateCommissionRate(decimal commissionRatePercent)
    {
        if (commissionRatePercent < 0m || commissionRatePercent > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionRatePercent));
        }
    }
}

public sealed class AffiliateReferral : Entity
{
    private AffiliateReferral()
    {
    }

    public int AffiliateId { get; private set; }

    public int ReferredCustomerId { get; private set; }

    public int StoreId { get; private set; }

    public DateTime ReferredAtUtc { get; private set; }

    public static AffiliateReferral Create(int affiliateId, int referredCustomerId, int storeId)
    {
        if (affiliateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(affiliateId));
        }

        if (referredCustomerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referredCustomerId));
        }

        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        return new AffiliateReferral
        {
            AffiliateId = affiliateId,
            ReferredCustomerId = referredCustomerId,
            StoreId = storeId,
            ReferredAtUtc = DateTime.UtcNow
        };
    }
}

public sealed class AffiliateCommissionAccount : AggregateRoot
{
    public const int CurrencyCodeMaxLength = 8;
    public const int IdempotencyKeyMaxLength = 128;

    private readonly List<AffiliateCommissionTransaction> _transactions = [];

    private AffiliateCommissionAccount()
    {
    }

    public int AffiliateId { get; private set; }

    public int StoreId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal Balance { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<AffiliateCommissionTransaction> Transactions => _transactions;

    public static AffiliateCommissionAccount Create(int affiliateId, int storeId, string currencyCode)
    {
        if (affiliateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(affiliateId));
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
        return new AffiliateCommissionAccount
        {
            AffiliateId = affiliateId,
            StoreId = storeId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Balance = 0m,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public AffiliateCommissionTransaction PostTransaction(
        AffiliateCommissionTransactionType type,
        decimal amountDelta,
        string idempotencyKey,
        AffiliateCommissionReferenceType referenceType,
        int? referenceId,
        string? reason)
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

        if (amountDelta == 0m && type is not AffiliateCommissionTransactionType.Adjust)
        {
            throw new ArgumentOutOfRangeException(nameof(amountDelta));
        }

        var newBalance = Balance + amountDelta;
        if (newBalance < 0m)
        {
            throw new InvalidOperationException("Insufficient affiliate commission balance.");
        }

        var transaction = AffiliateCommissionTransaction.Create(
            Id,
            type,
            amountDelta,
            newBalance,
            CurrencyCode,
            normalizedKey,
            referenceType,
            referenceId,
            reason);

        Balance = newBalance;
        _transactions.Add(transaction);
        UpdatedAtUtc = DateTime.UtcNow;
        return transaction;
    }
}

public sealed class AffiliateCommissionTransaction : Entity
{
    public const int ReasonMaxLength = 500;

    private AffiliateCommissionTransaction()
    {
    }

    public int AffiliateCommissionAccountId { get; private set; }

    public AffiliateCommissionTransactionType Type { get; private set; }

    public decimal AmountDelta { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public AffiliateCommissionReferenceType ReferenceType { get; private set; }

    public int? ReferenceId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static AffiliateCommissionTransaction Create(
        int affiliateCommissionAccountId,
        AffiliateCommissionTransactionType type,
        decimal amountDelta,
        decimal balanceAfter,
        string currencyCode,
        string idempotencyKey,
        AffiliateCommissionReferenceType referenceType,
        int? referenceId,
        string? reason)
    {
        return new AffiliateCommissionTransaction
        {
            AffiliateCommissionAccountId = affiliateCommissionAccountId,
            Type = type,
            AmountDelta = amountDelta,
            BalanceAfter = balanceAfter,
            CurrencyCode = currencyCode,
            IdempotencyKey = idempotencyKey,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
