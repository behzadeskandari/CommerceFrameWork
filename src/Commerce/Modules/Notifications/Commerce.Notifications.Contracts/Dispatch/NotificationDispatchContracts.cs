using Commerce.Notifications.Domain.Enums;

namespace Commerce.Notifications.Contracts.Dispatch;

public sealed record NotificationEventRequest(
    NotificationEventType EventType,
    int? StoreId,
    int? CustomerId,
    int? LanguageId,
    string? RecipientEmail,
    string? RecipientPhone,
    IReadOnlyDictionary<string, string> Variables);

public interface INotificationEventPublisher
{
    Task PublishAsync(NotificationEventRequest request, CancellationToken cancellationToken = default);
}

public sealed record NotificationDeliveryRequest(
    NotificationChannel Channel,
    string Recipient,
    string Subject,
    string Body,
    bool IsHtml = false);

public sealed record NotificationDeliveryResult(
    bool Success,
    string? ErrorMessage = null);

public interface INotificationChannelProvider
{
    NotificationChannel Channel { get; }

    Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken = default);
}
