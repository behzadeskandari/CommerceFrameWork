namespace Commerce.Integration.Domain.Enums;

public enum WebhookDeliveryStatus
{
    Pending = 0,
    Delivering = 1,
    Succeeded = 2,
    Failed = 3,
    DeadLetter = 4
}

public enum ApiClientStatus
{
    Active = 0,
    Revoked = 1
}
