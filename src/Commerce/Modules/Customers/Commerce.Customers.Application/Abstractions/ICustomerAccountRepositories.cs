using Commerce.Customers.Domain.Entities;

namespace Commerce.Customers.Application.Abstractions;

public interface ICustomerPreferenceRepository
{
    Task<IReadOnlyList<CustomerPreference>> ListByCustomerAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<CustomerPreference?> GetByKeyAsync(
        int customerId,
        int? storeId,
        string preferenceKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerPreference preference, CancellationToken cancellationToken = default);

    Task UpdateAsync(CustomerPreference preference, CancellationToken cancellationToken = default);
}

public interface ICustomerSegmentRepository
{
    Task<CustomerSegment?> GetByIdWithRulesAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerSegment>> ListAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddAsync(CustomerSegment segment, CancellationToken cancellationToken = default);

    Task UpdateAsync(CustomerSegment segment, CancellationToken cancellationToken = default);

    Task DeleteAsync(CustomerSegment segment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerSegmentMembership>> ListMembershipsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task AddMembershipAsync(CustomerSegmentMembership membership, CancellationToken cancellationToken = default);

    Task RemoveMembershipsForCustomerAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);
}

public interface ILoyaltyRepository
{
    Task<LoyaltyAccount?> GetAccountWithTransactionsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<LoyaltyAccount?> GetAccountByIdWithTransactionsAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    Task AddAccountAsync(LoyaltyAccount account, CancellationToken cancellationToken = default);

    Task UpdateAccountAsync(LoyaltyAccount account, CancellationToken cancellationToken = default);

    Task<LoyaltyReward?> GetRewardByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoyaltyReward>> ListRewardsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddRewardAsync(LoyaltyReward reward, CancellationToken cancellationToken = default);

    Task UpdateRewardAsync(LoyaltyReward reward, CancellationToken cancellationToken = default);

    Task DeleteRewardAsync(LoyaltyReward reward, CancellationToken cancellationToken = default);

    Task<LoyaltyRewardRedemption?> GetRedemptionByIdempotencyKeyAsync(
        int customerId,
        int storeId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddRedemptionAsync(LoyaltyRewardRedemption redemption, CancellationToken cancellationToken = default);

    Task UpdateRedemptionAsync(LoyaltyRewardRedemption redemption, CancellationToken cancellationToken = default);
}

public interface IStoreCreditRepository
{
    Task<StoreCreditAccount?> GetAccountWithTransactionsAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default);

    Task AddAccountAsync(StoreCreditAccount account, CancellationToken cancellationToken = default);

    Task UpdateAccountAsync(StoreCreditAccount account, CancellationToken cancellationToken = default);
}

public interface ICustomerActivityRepository
{
    Task<IReadOnlyList<CustomerActivityLog>> ListAsync(
        int customerId,
        int? storeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerActivityLog activity, CancellationToken cancellationToken = default);
}

public interface ICustomerPurchaseHistoryReader
{
    Task<IReadOnlyList<CustomerPurchaseHistoryRecord>> ListByCustomerAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerPurchaseHistoryRecord(
    int OrderId,
    string OrderNumber,
    decimal GrandTotal,
    string CurrencyCode,
    string Status,
    DateTime CreatedAtUtc);
