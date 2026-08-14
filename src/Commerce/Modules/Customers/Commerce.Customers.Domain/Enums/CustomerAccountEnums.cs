namespace Commerce.Customers.Domain.Enums;

public enum LoyaltyTransactionType
{
    Earn = 0,
    Spend = 1,
    Expire = 2,
    Adjust = 3,
    Refund = 4
}

public enum StoreCreditTransactionType
{
    Credit = 0,
    Debit = 1,
    Expire = 2,
    Adjust = 3,
    Refund = 4
}

public enum CustomerActivityType
{
    Registered = 0,
    ProfileUpdated = 1,
    Login = 2,
    OrderPlaced = 3,
    PointsEarned = 4,
    PointsSpent = 5,
    RewardRedeemed = 6,
    StoreCreditApplied = 7,
    PreferenceUpdated = 8,
    SegmentAssigned = 9,
    GroupAssigned = 10
}

public enum CustomerSegmentRuleType
{
    CustomerGroup = 0,
    MinOrderCount = 1,
    MinLifetimeSpend = 2
}

public enum LoyaltyRewardRedemptionStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2
}

public enum CustomerAccountReferenceType
{
    None = 0,
    Order = 1,
    Reward = 2,
    Manual = 3,
    Refund = 4
}
