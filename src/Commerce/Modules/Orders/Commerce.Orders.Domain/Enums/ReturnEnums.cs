namespace Commerce.Orders.Domain.Enums;

public enum ReturnStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
    ShipmentPending = 3,
    Received = 4,
    Restocked = 5,
    Refunded = 6,
    Completed = 7,
    Cancelled = 8
}

public enum ReturnResolutionType
{
    Refund = 0,
    Replacement = 1
}
