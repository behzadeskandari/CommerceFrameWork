using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Contracts.Affiliates;

public sealed record AffiliateSummaryDto(
    int Id,
    int CustomerId,
    int StoreId,
    string ReferralCode,
    decimal CommissionRatePercent,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record AffiliateDetailDto(
    int Id,
    int CustomerId,
    int StoreId,
    string ReferralCode,
    decimal CommissionRatePercent,
    bool IsActive,
    decimal CommissionBalance,
    string CurrencyCode,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record AffiliateCommissionTransactionDto(
    int Id,
    AffiliateCommissionTransactionType Type,
    decimal AmountDelta,
    decimal BalanceAfter,
    string CurrencyCode,
    string? Reason,
    DateTime CreatedAtUtc);

public sealed record AffiliateReferralDto(
    int Id,
    int AffiliateId,
    int ReferredCustomerId,
    int StoreId,
    DateTime ReferredAtUtc);

public sealed record CreateAffiliateRequest(
    int CustomerId,
    int StoreId,
    string ReferralCode,
    decimal CommissionRatePercent,
    bool IsActive);

public sealed record UpdateAffiliateRequest(
    decimal CommissionRatePercent,
    bool IsActive);

public sealed record AffiliateValidationResult(
    bool IsValid,
    int? AffiliateId,
    string? NormalizedReferralCode,
    IReadOnlyList<string> Errors);

public interface IAffiliateAdminService
{
    Task<Result<IReadOnlyList<AffiliateSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<AffiliateDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<AffiliateDetailDto>> CreateAsync(
        CreateAffiliateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AffiliateDetailDto>> UpdateAsync(
        int id,
        UpdateAffiliateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AffiliateCommissionTransactionDto>>> ListCommissionsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AffiliateReferralDto>>> ListReferralsAsync(
        int id,
        CancellationToken cancellationToken = default);
}

public interface IAffiliateValidationService
{
    Task<AffiliateValidationResult> ValidateReferralCodeAsync(
        string referralCode,
        int storeId,
        CancellationToken cancellationToken = default);
}

public interface IAffiliateReader
{
    Task<Result<AffiliateDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAffiliateReferralService
{
    Task<Result<AffiliateReferralDto>> RecordReferralAsync(
        int affiliateId,
        int referredCustomerId,
        int storeId,
        CancellationToken cancellationToken = default);
}

public interface IAffiliateCommissionService
{
    Task<Result<AffiliateCommissionTransactionDto>> EarnCommissionAsync(
        int affiliateId,
        int storeId,
        string currencyCode,
        decimal orderTotal,
        decimal commissionRatePercent,
        int orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
