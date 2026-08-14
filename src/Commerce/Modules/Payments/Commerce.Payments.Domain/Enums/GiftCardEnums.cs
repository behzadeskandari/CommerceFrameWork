namespace Commerce.Payments.Domain.Enums;

public enum GiftCardTransactionType
{
    Issue = 0,
    Redeem = 1,
    Refund = 2,
    Adjust = 3,
    Expire = 4
}

public enum GiftCardReferenceType
{
    None = 0,
    Order = 1,
    Manual = 2,
    Refund = 3
}
