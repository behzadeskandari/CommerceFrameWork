using Commerce.Framework.Core.Results;
using Commerce.Payments.Domain.Enums;

namespace Commerce.Payments.Contracts.GiftCards;

public sealed record GiftCardSummaryDto(
    int Id,
    string Code,
    int StoreId,
    string CurrencyCode,
    decimal InitialAmount,
    decimal Balance,
    bool IsActive,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc);

public sealed record GiftCardDetailDto(
    int Id,
    string Code,
    int StoreId,
    string CurrencyCode,
    decimal InitialAmount,
    decimal Balance,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    string? RecipientEmail,
    int? PurchasedByCustomerId,
    int? RecipientCustomerId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record GiftCardTransactionDto(
    int Id,
    GiftCardTransactionType Type,
    decimal AmountDelta,
    decimal BalanceAfter,
    string CurrencyCode,
    string? Reason,
    DateTime CreatedAtUtc);

public sealed record CreateGiftCardRequest(
    string Code,
    int StoreId,
    string CurrencyCode,
    decimal InitialAmount,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    string? RecipientEmail,
    int? PurchasedByCustomerId,
    int? RecipientCustomerId);

public sealed record UpdateGiftCardRequest(
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    string? RecipientEmail,
    int? RecipientCustomerId);

public sealed record GiftCardValidationRequest(
    string Code,
    int StoreId,
    string CurrencyCode,
    decimal RequestedAmount,
    DateTime CurrentTimeUtc);

public sealed record GiftCardValidationResult(
    bool IsValid,
    string? NormalizedCode,
    int? GiftCardId,
    decimal AvailableBalance,
    IReadOnlyList<string> Errors);

public sealed record GiftCardRedemptionRequest(
    string Code,
    int StoreId,
    string CurrencyCode,
    decimal Amount,
    int OrderId,
    string IdempotencyKey);

public sealed record GiftCardRedemptionResult(
    bool Success,
    string? ErrorMessage,
    int? GiftCardId = null,
    decimal AmountApplied = 0m);

public interface IGiftCardAdminService
{
    Task<Result<IReadOnlyList<GiftCardSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<GiftCardDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<GiftCardDetailDto>> CreateAsync(
        CreateGiftCardRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<GiftCardDetailDto>> UpdateAsync(
        int id,
        UpdateGiftCardRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<GiftCardTransactionDto>>> ListTransactionsAsync(
        int id,
        CancellationToken cancellationToken = default);
}

public interface IGiftCardValidationService
{
    Task<GiftCardValidationResult> ValidateAsync(
        GiftCardValidationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IGiftCardRedemptionService
{
    Task<GiftCardRedemptionResult> TryRedeemAsync(
        GiftCardRedemptionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IGiftCardReader
{
    Task<Result<GiftCardDetailDto>> GetByCodeAsync(
        string code,
        int storeId,
        CancellationToken cancellationToken = default);
}
