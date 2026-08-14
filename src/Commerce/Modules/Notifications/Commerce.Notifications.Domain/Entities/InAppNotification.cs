using Commerce.Framework.Core.Entities;

namespace Commerce.Notifications.Domain.Entities;

public sealed class InAppNotification : AggregateRoot
{
    public const int TitleMaxLength = 200;
    public const int BodyMaxLength = 4000;

    public int CustomerId { get; private set; }

    public int? StoreId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public bool IsRead { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ReadAtUtc { get; private set; }

    public static InAppNotification Create(int customerId, int? storeId, string title, string body)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(title) || title.Length > TitleMaxLength)
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(body) || body.Length > BodyMaxLength)
        {
            throw new ArgumentException("Body is required.", nameof(body));
        }

        return new InAppNotification
        {
            CustomerId = customerId,
            StoreId = storeId,
            Title = title.Trim(),
            Body = body,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }
}
