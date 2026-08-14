namespace Commerce.Promotions.Domain.Enums;

public enum PromotionConditionType
{
    MinimumCartSubtotal = 1,
    MinimumQuantity = 2,
    CustomerGroup = 3,
    ProductInCart = 4,
    CategoryInCart = 5,
    ProductRestriction = 6,
    CategoryRestriction = 7,
    StoreRestriction = 8,
    UsageLimitRemaining = 9,
    PerCustomerUsageRemaining = 10
}

public enum PromotionActionType
{
    PercentageDiscount = 1,
    FixedAmountDiscount = 2,
    BuyXGetY = 3,
    ApplyLinkedDiscount = 4
}

public enum PromotionCombinationRule
{
    Exclusive = 1,
    Stackable = 2,
    SameGroupExclusive = 3
}

public enum PromotionTargetScope
{
    Line = 1,
    Cart = 2
}
