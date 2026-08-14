namespace Commerce.Shipping.Domain.Enums;

public enum ShippingRateType
{
    Flat = 1,
    WeightBased = 2,
    OrderSubtotalBased = 3,
    QuantityBased = 4
}

public enum PostalRuleType
{
    Exact = 1,
    Prefix = 2,
    Range = 3
}

public enum ShipmentStatus
{
    Pending = 0,
    Shipped = 1,
    Delivered = 2,
    Cancelled = 3
}
