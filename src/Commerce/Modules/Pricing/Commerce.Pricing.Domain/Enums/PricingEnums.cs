namespace Commerce.Pricing.Domain.Enums;

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public enum DiscountTargetType
{
    Product = 1,
    Variant = 2,
    Offer = 3,
    Category = 4,
    Cart = 5
}

public enum DiscountApplicationScope
{
    Line = 1,
    Cart = 2
}

public enum CustomerEligibility
{
    All = 1,
    Authenticated = 2,
    Guest = 3,
    SpecificCustomer = 4,
    CustomerGroup = 5
}

public enum StackingMode
{
    NonStackable = 1,
    Stackable = 2
}
