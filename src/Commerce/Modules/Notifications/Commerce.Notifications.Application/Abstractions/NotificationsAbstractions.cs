using Commerce.Notifications.Domain.Entities;
using Commerce.Notifications.Domain.Enums;

namespace Commerce.Notifications.Application.Abstractions;

public interface INotificationsRepository
{
    Task<IReadOnlyList<NotificationTemplate>> ListTemplatesAsync(
        int? storeId,
        NotificationEventType? eventType,
        CancellationToken cancellationToken = default);

    Task<NotificationTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<NotificationTemplate?> GetTemplateBySystemNameAsync(string systemName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationTemplate>> GetEnabledTemplatesForEventAsync(
        NotificationEventType eventType,
        int? storeId,
        int? languageId,
        CancellationToken cancellationToken = default);

    Task AddTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    Task SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    Task DeleteTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    Task AddLogAsync(NotificationLog log, CancellationToken cancellationToken = default);

    Task SaveLogAsync(NotificationLog log, CancellationToken cancellationToken = default);

    Task<NotificationLog?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationLog>> ListLogsAsync(
        int? storeId,
        NotificationDeliveryStatus? status,
        int? customerId,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationLog>> GetRetryCandidatesAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default);

    Task AddInAppNotificationAsync(InAppNotification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InAppNotification>> ListUnreadInAppAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<InAppNotification?> GetInAppByIdAsync(int id, CancellationToken cancellationToken = default);

    Task SaveInAppNotificationAsync(InAppNotification notification, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
