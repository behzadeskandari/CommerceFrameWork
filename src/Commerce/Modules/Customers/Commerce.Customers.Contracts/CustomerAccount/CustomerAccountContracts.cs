using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Contracts.CustomerAccount;

public sealed record CustomerPreferenceDto(
    int Id,
    int CustomerId,
    int? StoreId,
    string PreferenceKey,
    string PreferenceValue,
    DateTime UpdatedAtUtc);

public sealed record UpsertCustomerPreferenceRequest(
    string PreferenceKey,
    string PreferenceValue,
    int? StoreId = null);

public sealed record CustomerSegmentRuleDto(
    int Id,
    CustomerSegmentRuleType RuleType,
    int? CustomerGroupId,
    int? MinOrderCount,
    decimal? MinLifetimeSpend);

public sealed record CustomerSegmentSummaryDto(
    int Id,
    int StoreId,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CustomerSegmentDetailDto(
    int Id,
    int StoreId,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<CustomerSegmentRuleDto> Rules,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateCustomerSegmentRequest(
    int StoreId,
    string Name,
    string? Description,
    IReadOnlyList<CreateCustomerSegmentRuleRequest> Rules);

public sealed record CreateCustomerSegmentRuleRequest(
    CustomerSegmentRuleType RuleType,
    int? CustomerGroupId,
    int? MinOrderCount,
    decimal? MinLifetimeSpend);

public sealed record UpdateCustomerSegmentRequest(
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<CreateCustomerSegmentRuleRequest> Rules);

public sealed record LoyaltyAccountDto(
    int Id,
    int CustomerId,
    int StoreId,
    int PointsBalance,
    DateTime UpdatedAtUtc);

public sealed record LoyaltyTransactionDto(
    int Id,
    LoyaltyTransactionType Type,
    int PointsDelta,
    int BalanceAfter,
    string? Reason,
    DateTime? ExpiresAtUtc,
    bool IsExpired,
    DateTime CreatedAtUtc);

public sealed record LoyaltyRewardDto(
    int Id,
    int StoreId,
    string Name,
    string? Description,
    int PointsCost,
    bool IsActive);

public sealed record CreateLoyaltyRewardRequest(
    int StoreId,
    string Name,
    int PointsCost,
    string? Description);

public sealed record UpdateLoyaltyRewardRequest(
    string Name,
    int PointsCost,
    string? Description,
    bool IsActive);

public sealed record RedeemLoyaltyRewardRequest(int RewardId);

public sealed record LoyaltyRewardRedemptionDto(
    int Id,
    int RewardId,
    int PointsSpent,
    LoyaltyRewardRedemptionStatus Status,
    DateTime CreatedAtUtc);

public sealed record StoreCreditAccountDto(
    int Id,
    int CustomerId,
    int StoreId,
    string CurrencyCode,
    decimal Balance,
    DateTime UpdatedAtUtc);

public sealed record StoreCreditTransactionDto(
    int Id,
    StoreCreditTransactionType Type,
    decimal AmountDelta,
    decimal BalanceAfter,
    string CurrencyCode,
    string? Reason,
    DateTime? ExpiresAtUtc,
    bool IsExpired,
    DateTime CreatedAtUtc);

public sealed record ApplyStoreCreditRequest(
    decimal Amount,
    int? OrderId,
    string? Reason);

public sealed record CreditStoreCreditRequest(
    decimal Amount,
    string? Reason,
    DateTime? ExpiresAtUtc);

public sealed record CustomerActivityDto(
    int Id,
    int? StoreId,
    CustomerActivityType ActivityType,
    string Summary,
    string? DetailsJson,
    DateTime CreatedAtUtc);

public sealed record CustomerAccountOverviewDto(
    CustomerPreferenceDto[] Preferences,
    LoyaltyAccountDto? Loyalty,
    StoreCreditAccountDto? StoreCredit,
    CustomerActivityDto[] RecentActivity);

public sealed record AssignCustomerGroupRequest(int? CustomerGroupId);

public sealed record UpdateCustomerTaxProfileRequest(
    bool IsTaxExempt,
    string? TaxRegistrationNumber);

public sealed record CustomerPurchaseHistoryItemDto(
    int OrderId,
    string OrderNumber,
    decimal GrandTotal,
    string CurrencyCode,
    string Status,
    DateTime CreatedAtUtc);

public interface ICustomerPreferenceService
{
    Task<Result<IReadOnlyList<CustomerPreferenceDto>>> ListAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerPreferenceDto>> UpsertAsync(
        int customerId,
        UpsertCustomerPreferenceRequest request,
        CancellationToken cancellationToken = default);
}

public interface ICustomerSegmentAdminService
{
    Task<Result<IReadOnlyList<CustomerSegmentSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerSegmentDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<CustomerSegmentDetailDto>> CreateAsync(
        CreateCustomerSegmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerSegmentDetailDto>> UpdateAsync(
        int id,
        UpdateCustomerSegmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerSegmentSummaryDto>>> EvaluateCustomerSegmentsAsync(
        int customerId,
        int storeId,
        int? customerGroupId,
        int orderCount,
        decimal lifetimeSpend,
        CancellationToken cancellationToken = default);
}

public interface ILoyaltyService
{
    Task<Result<LoyaltyAccountDto>> GetAccountAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LoyaltyTransactionDto>>> ListTransactionsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Result<LoyaltyTransactionDto>> EarnAsync(
        int customerId,
        int storeId,
        int points,
        string idempotencyKey,
        CustomerAccountReferenceType referenceType,
        int? referenceId,
        string? reason,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<Result<LoyaltyTransactionDto>> SpendAsync(
        int customerId,
        int storeId,
        int points,
        string idempotencyKey,
        CustomerAccountReferenceType referenceType,
        int? referenceId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<Result<int>> ExpirePointsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Result<LoyaltyRewardRedemptionDto>> RedeemRewardAsync(
        int customerId,
        int storeId,
        RedeemLoyaltyRewardRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public interface ILoyaltyRewardAdminService
{
    Task<Result<IReadOnlyList<LoyaltyRewardDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<LoyaltyRewardDto>> CreateAsync(
        CreateLoyaltyRewardRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LoyaltyRewardDto>> UpdateAsync(
        int id,
        UpdateLoyaltyRewardRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IStoreCreditService
{
    Task<Result<StoreCreditAccountDto>> GetAccountAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoreCreditTransactionDto>>> ListTransactionsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Result<StoreCreditTransactionDto>> CreditAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CreditStoreCreditRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<StoreCreditTransactionDto>> DebitAsync(
        int customerId,
        int storeId,
        string currencyCode,
        ApplyStoreCreditRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<decimal>> ExpireCreditAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default);
}

public interface IStoreCreditReader
{
    Task<Result<StoreCreditAccountDto>> GetAvailableCreditAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default);
}

public interface ICustomerActivityService
{
    Task<Result<IReadOnlyList<CustomerActivityDto>>> ListAsync(
        int customerId,
        int? storeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task LogAsync(
        int customerId,
        int? storeId,
        CustomerActivityType activityType,
        string summary,
        string? detailsJson = null,
        CancellationToken cancellationToken = default);
}

public interface ICustomerAccountAdminService
{
    Task<Result> AssignCustomerGroupAsync(
        int customerId,
        AssignCustomerGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateTaxProfileAsync(
        int customerId,
        UpdateCustomerTaxProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerPurchaseHistoryItemDto>>> GetPurchaseHistoryAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}

public interface ICustomerAccountStorefrontService
{
    Task<Result<CustomerAccountOverviewDto>> GetOverviewAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LoyaltyRewardDto>>> ListAvailableRewardsAsync(
        CancellationToken cancellationToken = default);
}
