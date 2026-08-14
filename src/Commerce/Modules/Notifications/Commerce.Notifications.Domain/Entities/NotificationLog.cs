using Commerce.Framework.Core.Entities;
using Commerce.Notifications.Domain.Enums;

namespace Commerce.Notifications.Domain.Entities;

public sealed class NotificationLog : AggregateRoot
{
    public const int RecipientMaxLength = 256;
    public const int SubjectMaxLength = 500;
    public const int ErrorMaxLength = 2000;

    public int? TemplateId { get; private set; }

    public NotificationEventType EventType { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public int? StoreId { get; private set; }

    public int? CustomerId { get; private set; }

    public string Recipient { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public NotificationDeliveryStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public int MaxAttempts { get; private set; }

    public DateTime? NextRetryAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public static NotificationLog CreatePending(
        int? templateId,
        NotificationEventType eventType,
        NotificationChannel channel,
        int? storeId,
        int? customerId,
        string recipient,
        string subject,
        string body,
        int maxAttempts = 3)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        }

        var utcNow = DateTime.UtcNow;
        return new NotificationLog
        {
            TemplateId = templateId,
            EventType = eventType,
            Channel = channel,
            StoreId = storeId,
            CustomerId = customerId,
            Recipient = recipient.Trim(),
            Subject = subject.Trim(),
            Body = body,
            Status = NotificationDeliveryStatus.Pending,
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            NextRetryAtUtc = null,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void MarkSent()
    {
        Status = NotificationDeliveryStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        NextRetryAtUtc = null;
        LastError = null;
        Touch();
    }

    public void MarkFailed(string error, DateTime? nextRetryAtUtc, bool incrementAttempt = true)
    {
        if (incrementAttempt)
        {
            AttemptCount++;
        }
        LastError = Truncate(error, ErrorMaxLength);
        UpdatedAtUtc = DateTime.UtcNow;

        if (AttemptCount >= MaxAttempts || !nextRetryAtUtc.HasValue)
        {
            Status = NotificationDeliveryStatus.Failed;
            NextRetryAtUtc = null;
            return;
        }

        Status = NotificationDeliveryStatus.Pending;
        NextRetryAtUtc = nextRetryAtUtc;
    }

    public void MarkCancelled(string reason)
    {
        Status = NotificationDeliveryStatus.Cancelled;
        LastError = Truncate(reason, ErrorMaxLength);
        NextRetryAtUtc = null;
        Touch();
    }

    public void RecordAttempt()
    {
        AttemptCount++;
        Touch();
    }

    public bool IsEligibleForRetry(DateTime utcNow) =>
        Status is NotificationDeliveryStatus.Pending or NotificationDeliveryStatus.Failed &&
        AttemptCount < MaxAttempts &&
        NextRetryAtUtc.HasValue &&
        utcNow >= NextRetryAtUtc.Value;

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
