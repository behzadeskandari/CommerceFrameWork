using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Domain.Entities;
using Commerce.Notifications.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Notifications.Infrastructure.Persistence.Repositories;

public sealed class EfNotificationsRepository(CommerceDbContext dbContext) : INotificationsRepository
{
    public Task<IReadOnlyList<NotificationTemplate>> ListTemplatesAsync(
        int? storeId,
        NotificationEventType? eventType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<NotificationTemplate>().Where(x => !x.IsDeleted);
        if (storeId.HasValue)
        {
            query = query.Where(x => !x.StoreId.HasValue || x.StoreId.Value == storeId.Value);
        }

        if (eventType.HasValue)
        {
            query = query.Where(x => x.EventType == eventType.Value);
        }

        return query.OrderBy(x => x.SystemName).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationTemplate>)t.Result, cancellationToken);
    }

    public Task<NotificationTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationTemplate>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<NotificationTemplate?> GetTemplateBySystemNameAsync(string systemName, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(x => x.SystemName == systemName.Trim().ToLowerInvariant() && !x.IsDeleted, cancellationToken);

    public Task<IReadOnlyList<NotificationTemplate>> GetEnabledTemplatesForEventAsync(
        NotificationEventType eventType,
        int? storeId,
        int? languageId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<NotificationTemplate>()
            .Where(x => !x.IsDeleted && x.IsEnabled && x.EventType == eventType);

        if (storeId.HasValue)
        {
            query = query.Where(x => !x.StoreId.HasValue || x.StoreId.Value == storeId.Value);
        }

        if (languageId.HasValue)
        {
            query = query.Where(x => !x.LanguageId.HasValue || x.LanguageId.Value == languageId.Value);
        }

        return query.ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationTemplate>)t.Result, cancellationToken);
    }

    public async Task AddTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationTemplate>().Add(template);

    public async Task SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationTemplate>().Update(template);

    public async Task DeleteTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationTemplate>().Update(template);

    public async Task AddLogAsync(NotificationLog log, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationLog>().Add(log);

    public async Task SaveLogAsync(NotificationLog log, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationLog>().Update(log);

    public Task<NotificationLog?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationLog>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<IReadOnlyList<NotificationLog>> ListLogsAsync(
        int? storeId,
        NotificationDeliveryStatus? status,
        int? customerId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<NotificationLog>().AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        return query.OrderByDescending(x => x.CreatedAtUtc).Take(take).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationLog>)t.Result, cancellationToken);
    }

    public Task<IReadOnlyList<NotificationLog>> GetRetryCandidatesAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default) =>
        dbContext.Set<NotificationLog>()
            .Where(x =>
                x.Status == NotificationDeliveryStatus.Pending &&
                x.NextRetryAtUtc.HasValue &&
                x.NextRetryAtUtc.Value <= utcNow &&
                x.AttemptCount < x.MaxAttempts)
            .OrderBy(x => x.NextRetryAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationLog>)t.Result, cancellationToken);

    public async Task AddInAppNotificationAsync(InAppNotification notification, CancellationToken cancellationToken = default) =>
        dbContext.Set<InAppNotification>().Add(notification);

    public Task<IReadOnlyList<InAppNotification>> ListUnreadInAppAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<InAppNotification>()
            .Where(x => x.CustomerId == customerId && !x.IsRead);

        if (storeId.HasValue)
        {
            query = query.Where(x => !x.StoreId.HasValue || x.StoreId.Value == storeId.Value);
        }

        return query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<InAppNotification>)t.Result, cancellationToken);
    }

    public Task<InAppNotification?> GetInAppByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<InAppNotification>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task SaveInAppNotificationAsync(InAppNotification notification, CancellationToken cancellationToken = default) =>
        dbContext.Set<InAppNotification>().Update(notification);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
