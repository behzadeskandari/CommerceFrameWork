namespace Commerce.Orders.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}

public enum FulfillmentStatus
{
    Unfulfilled = 0,
    PartiallyFulfilled = 1,
    Fulfilled = 2,
    Cancelled = 3
}

public enum OrderStatusHistoryType
{
    Order = 0,
    Payment = 1,
    Fulfillment = 2
}
