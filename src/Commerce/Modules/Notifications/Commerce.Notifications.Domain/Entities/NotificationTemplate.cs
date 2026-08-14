using Commerce.Framework.Core.Entities;
using Commerce.Notifications.Domain.Enums;

namespace Commerce.Notifications.Domain.Entities;

public sealed class NotificationTemplate : AggregateRoot
{
    public const int SystemNameMaxLength = 128;
    public const int SubjectMaxLength = 500;
    public const int BodyMaxLength = 16000;
    public const int VariablesMaxLength = 2000;

    public string SystemName { get; private set; } = string.Empty;

    public NotificationEventType EventType { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public int? LanguageId { get; private set; }

    public int? StoreId { get; private set; }

    public string? VariablesJson { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static NotificationTemplate Create(
        string systemName,
        NotificationEventType eventType,
        NotificationChannel channel,
        string subject,
        string body,
        int? languageId,
        int? storeId,
        string? variablesJson,
        bool isEnabled)
    {
        ValidateSystemName(systemName);
        ValidateSubject(subject);
        ValidateBody(body);

        var utcNow = DateTime.UtcNow;
        return new NotificationTemplate
        {
            SystemName = systemName.Trim().ToLowerInvariant(),
            EventType = eventType,
            Channel = channel,
            Subject = subject.Trim(),
            Body = body,
            LanguageId = languageId,
            StoreId = storeId,
            VariablesJson = string.IsNullOrWhiteSpace(variablesJson) ? null : variablesJson.Trim(),
            IsEnabled = isEnabled,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(
        string subject,
        string body,
        int? languageId,
        int? storeId,
        string? variablesJson,
        bool isEnabled)
    {
        EnsureNotDeleted();
        ValidateSubject(subject);
        ValidateBody(body);

        Subject = subject.Trim();
        Body = body;
        LanguageId = languageId;
        StoreId = storeId;
        VariablesJson = string.IsNullOrWhiteSpace(variablesJson) ? null : variablesJson.Trim();
        IsEnabled = isEnabled;
        Touch();
    }

    public void Enable()
    {
        EnsureNotDeleted();
        IsEnabled = true;
        Touch();
    }

    public void Disable()
    {
        EnsureNotDeleted();
        IsEnabled = false;
        Touch();
    }

    public void SoftDelete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        IsEnabled = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Notification template has been deleted.");
        }
    }

    private static void ValidateSystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName) || systemName.Length > SystemNameMaxLength)
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }
    }

    private static void ValidateSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > SubjectMaxLength)
        {
            throw new ArgumentException("Subject is required.", nameof(subject));
        }
    }

    private static void ValidateBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > BodyMaxLength)
        {
            throw new ArgumentException("Body is required.", nameof(body));
        }
    }
}
