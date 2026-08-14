namespace Commerce.Customers.Domain.Enums;

public enum AffiliateCommissionTransactionType
{
    Earn = 0,
    Payout = 1,
    Adjust = 2,
    Refund = 3
}

public enum AffiliateCommissionReferenceType
{
    None = 0,
    Order = 1,
    Manual = 2,
    Payout = 3
}
