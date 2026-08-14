using Commerce.Framework.Core.Entities;
using Commerce.Payments.Domain.Enums;

namespace Commerce.Payments.Domain.Entities;

public sealed class GiftCard : AggregateRoot
{
    public const int CodeMaxLength = 64;
    public const int CurrencyCodeMaxLength = 8;
    public const int IdempotencyKeyMaxLength = 128;
    public const int EmailMaxLength = 500;

    private readonly List<GiftCardTransaction> _transactions = [];

    private GiftCard()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public int StoreId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal InitialAmount { get; private set; }

    public decimal Balance { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? StartsAtUtc { get; private set; }

    public DateTime? ExpiresAtUtc { get; private set; }

    public string? RecipientEmail { get; private set; }

    public int? PurchasedByCustomerId { get; private set; }

    public int? RecipientCustomerId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<GiftCardTransaction> Transactions => _transactions;

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static GiftCard Create(
        string code,
        int storeId,
        string currencyCode,
        decimal initialAmount,
        bool isActive,
        DateTime? startsAtUtc,
        DateTime? expiresAtUtc,
        string? recipientEmail,
        int? purchasedByCustomerId,
        int? recipientCustomerId)
    {
        ValidateCode(code);
        ValidateStore(storeId);
        ValidateCurrency(currencyCode);
        ValidateAmount(initialAmount);
        ValidateDateRange(startsAtUtc, expiresAtUtc);

        var utcNow = DateTime.UtcNow;
        var normalized = NormalizeCode(code);
        var giftCard = new GiftCard
        {
            Code = normalized,
            StoreId = storeId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            InitialAmount = initialAmount,
            Balance = 0m,
            IsActive = isActive,
            StartsAtUtc = startsAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            RecipientEmail = NormalizeEmail(recipientEmail),
            PurchasedByCustomerId = purchasedByCustomerId,
            RecipientCustomerId = recipientCustomerId,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        giftCard.PostTransaction(
            GiftCardTransactionType.Issue,
            initialAmount,
            $"issue-{normalized}",
            GiftCardReferenceType.Manual,
            null,
            "Gift card issued.");

        return giftCard;
    }

    public void Update(
        bool isActive,
        DateTime? startsAtUtc,
        DateTime? expiresAtUtc,
        string? recipientEmail,
        int? recipientCustomerId)
    {
        EnsureNotDeleted();
        ValidateDateRange(startsAtUtc, expiresAtUtc);

        IsActive = isActive;
        StartsAtUtc = startsAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RecipientEmail = NormalizeEmail(recipientEmail);
        RecipientCustomerId = recipientCustomerId;
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
        (!ExpiresAtUtc.HasValue || utcNow <= ExpiresAtUtc.Value);

    public bool AppliesToStore(int storeId) => StoreId == storeId;

    public GiftCardTransaction PostTransaction(
        GiftCardTransactionType type,
        decimal amountDelta,
        string idempotencyKey,
        GiftCardReferenceType referenceType,
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

        if (amountDelta == 0m && type is not GiftCardTransactionType.Adjust)
        {
            throw new ArgumentOutOfRangeException(nameof(amountDelta));
        }

        var newBalance = Balance + amountDelta;
        if (newBalance < 0m)
        {
            throw new InvalidOperationException("Insufficient gift card balance.");
        }

        var transaction = GiftCardTransaction.Create(
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
        Touch();
        return transaction;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Gift card has been deleted.");
        }
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Gift card code is required.", nameof(code));
        }

        if (code.Trim().Length > CodeMaxLength)
        {
            throw new ArgumentException($"Gift card code cannot exceed {CodeMaxLength} characters.", nameof(code));
        }
    }

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }
    }

    private static void ValidateCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }
    }

    private static void ValidateDateRange(DateTime? startsAtUtc, DateTime? expiresAtUtc)
    {
        if (startsAtUtc.HasValue && expiresAtUtc.HasValue && startsAtUtc.Value > expiresAtUtc.Value)
        {
            throw new ArgumentException("Start date must be before end date.");
        }
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim();
}

public sealed class GiftCardTransaction : Entity
{
    public const int ReasonMaxLength = 500;

    private GiftCardTransaction()
    {
    }

    public int GiftCardId { get; private set; }

    public GiftCardTransactionType Type { get; private set; }

    public decimal AmountDelta { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public GiftCardReferenceType ReferenceType { get; private set; }

    public int? ReferenceId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static GiftCardTransaction Create(
        int giftCardId,
        GiftCardTransactionType type,
        decimal amountDelta,
        decimal balanceAfter,
        string currencyCode,
        string idempotencyKey,
        GiftCardReferenceType referenceType,
        int? referenceId,
        string? reason)
    {
        return new GiftCardTransaction
        {
            GiftCardId = giftCardId,
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
