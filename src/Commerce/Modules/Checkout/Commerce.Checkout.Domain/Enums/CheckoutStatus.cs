namespace Commerce.Checkout.Domain.Enums;

public enum CheckoutStatus
{
    Active = 0,
    RequiresReview = 1,
    ReadyForOrder = 2,
    Expired = 3,
    Completed = 4,
    Cancelled = 5
}
