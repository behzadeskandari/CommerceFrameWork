using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.CustomerAccount;

internal static class CustomerAccountMapper
{
    public static CustomerPreferenceDto MapPreference(CustomerPreference preference) =>
        new(
            preference.Id,
            preference.CustomerId,
            preference.StoreId,
            preference.PreferenceKey,
            preference.PreferenceValue,
            preference.UpdatedAtUtc);

    public static CustomerSegmentSummaryDto MapSegmentSummary(CustomerSegment segment) =>
        new(segment.Id, segment.StoreId, segment.Name, segment.IsActive, segment.CreatedAtUtc);

    public static CustomerSegmentDetailDto MapSegmentDetail(CustomerSegment segment) =>
        new(
            segment.Id,
            segment.StoreId,
            segment.Name,
            segment.Description,
            segment.IsActive,
            segment.Rules.Select(MapSegmentRule).ToList(),
            segment.CreatedAtUtc,
            segment.UpdatedAtUtc);

    public static CustomerSegmentRuleDto MapSegmentRule(CustomerSegmentRule rule) =>
        new(rule.Id, rule.RuleType, rule.CustomerGroupId, rule.MinOrderCount, rule.MinLifetimeSpend);

    public static LoyaltyAccountDto MapLoyaltyAccount(LoyaltyAccount account) =>
        new(account.Id, account.CustomerId, account.StoreId, account.PointsBalance, account.UpdatedAtUtc);

    public static LoyaltyTransactionDto MapLoyaltyTransaction(LoyaltyTransaction transaction) =>
        new(
            transaction.Id,
            transaction.Type,
            transaction.PointsDelta,
            transaction.BalanceAfter,
            transaction.Reason,
            transaction.ExpiresAtUtc,
            transaction.IsExpired,
            transaction.CreatedAtUtc);

    public static LoyaltyRewardDto MapReward(LoyaltyReward reward) =>
        new(reward.Id, reward.StoreId, reward.Name, reward.Description, reward.PointsCost, reward.IsActive);

    public static StoreCreditAccountDto MapStoreCreditAccount(StoreCreditAccount account) =>
        new(account.Id, account.CustomerId, account.StoreId, account.CurrencyCode, account.Balance, account.UpdatedAtUtc);

    public static StoreCreditTransactionDto MapStoreCreditTransaction(StoreCreditTransaction transaction) =>
        new(
            transaction.Id,
            transaction.Type,
            transaction.AmountDelta,
            transaction.BalanceAfter,
            transaction.CurrencyCode,
            transaction.Reason,
            transaction.ExpiresAtUtc,
            transaction.IsExpired,
            transaction.CreatedAtUtc);

    public static CustomerActivityDto MapActivity(CustomerActivityLog activity) =>
        new(
            activity.Id,
            activity.StoreId,
            activity.ActivityType,
            activity.Summary,
            activity.DetailsJson,
            activity.CreatedAtUtc);

    public static CustomerSegmentRule CreateRule(CreateCustomerSegmentRuleRequest request) =>
        request.RuleType switch
        {
            CustomerSegmentRuleType.CustomerGroup when request.CustomerGroupId is > 0 =>
                CustomerSegmentRule.ForCustomerGroup(request.CustomerGroupId.Value),
            CustomerSegmentRuleType.MinOrderCount when request.MinOrderCount is > 0 =>
                CustomerSegmentRule.ForMinOrderCount(request.MinOrderCount.Value),
            CustomerSegmentRuleType.MinLifetimeSpend when request.MinLifetimeSpend is > 0 =>
                CustomerSegmentRule.ForMinLifetimeSpend(request.MinLifetimeSpend.Value),
            _ => throw new ArgumentException("Invalid segment rule payload.")
        };
}
