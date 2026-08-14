namespace Commerce.Notifications.Domain.Enums;

public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    InApp = 3
}

public enum NotificationEventType
{
    CustomerRegistered = 1,
    OrderCreated = 2,
    PaymentSucceeded = 3,
    PaymentFailed = 4,
    OrderCancelled = 5,
    ShipmentCreated = 6,
    RefundCreated = 7,
    DownloadAvailable = 8,
    ReturnRequested = 9,
    ReturnApproved = 10,
    ReturnRejected = 11,
    ReturnCompleted = 12
}

public enum NotificationDeliveryStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}
