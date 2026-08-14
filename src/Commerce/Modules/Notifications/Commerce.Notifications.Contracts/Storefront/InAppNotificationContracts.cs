namespace Commerce.Notifications.Contracts.Storefront;

public sealed record InAppNotificationDto(
    int Id,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public interface IInAppNotificationStorefrontService
{
    Task<IReadOnlyList<InAppNotificationDto>> ListUnreadAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(int customerId, int notificationId, CancellationToken cancellationToken = default);
}
