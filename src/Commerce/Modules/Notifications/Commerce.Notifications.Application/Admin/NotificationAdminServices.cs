using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Application.Dispatch;
using Commerce.Notifications.Contracts.Admin;
using Commerce.Notifications.Contracts.Storefront;
using Commerce.Notifications.Domain.Entities;
using Commerce.Notifications.Domain.Enums;

namespace Commerce.Notifications.Application.Admin;

public sealed class NotificationTemplateAdminService(INotificationsRepository repository) : INotificationTemplateAdminService
{
    public async Task<Result<IReadOnlyList<NotificationTemplateSummaryDto>>> ListAsync(
        int? storeId,
        NotificationEventType? eventType,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.ListTemplatesAsync(storeId, eventType, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<NotificationTemplateSummaryDto>>(items.Select(MapSummary).ToList());
    }

    public async Task<Result<NotificationTemplateDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await repository.GetTemplateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return template is null
            ? Result.Failure<NotificationTemplateDetailDto>(Error.NotFound("Template not found."))
            : Result.Success(MapDetail(template));
    }

    public async Task<Result<NotificationTemplateDetailDto>> CreateAsync(
        CreateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await repository.GetTemplateBySystemNameAsync(request.SystemName, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return Result.Failure<NotificationTemplateDetailDto>(Error.Conflict("Template system name already exists."));
            }

            var template = NotificationTemplate.Create(
                request.SystemName,
                request.EventType,
                request.Channel,
                request.Subject,
                request.Body,
                request.LanguageId,
                request.StoreId,
                request.VariablesJson,
                request.IsEnabled);

            await repository.AddTemplateAsync(template, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(template));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<NotificationTemplateDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<NotificationTemplateDetailDto>> UpdateAsync(
        int id,
        UpdateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await repository.GetTemplateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Result.Failure<NotificationTemplateDetailDto>(Error.NotFound("Template not found."));
        }

        try
        {
            template.Update(
                request.Subject,
                request.Body,
                request.LanguageId,
                request.StoreId,
                request.VariablesJson,
                request.IsEnabled);
            await repository.SaveTemplateAsync(template, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(template));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<NotificationTemplateDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await repository.GetTemplateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Result.Failure(Error.NotFound("Template not found."));
        }

        template.SoftDelete();
        await repository.SaveTemplateAsync(template, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await repository.GetTemplateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Result.Failure(Error.NotFound("Template not found."));
        }

        template.Enable();
        await repository.SaveTemplateAsync(template, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await repository.GetTemplateByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Result.Failure(Error.NotFound("Template not found."));
        }

        template.Disable();
        await repository.SaveTemplateAsync(template, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static NotificationTemplateSummaryDto MapSummary(NotificationTemplate template) =>
        new(template.Id, template.SystemName, template.EventType, template.Channel, template.LanguageId, template.StoreId, template.IsEnabled);

    private static NotificationTemplateDetailDto MapDetail(NotificationTemplate template) =>
        new(
            template.Id,
            template.SystemName,
            template.EventType,
            template.Channel,
            template.Subject,
            template.Body,
            template.LanguageId,
            template.StoreId,
            template.VariablesJson,
            template.IsEnabled,
            template.CreatedAtUtc,
            template.UpdatedAtUtc);
}

public sealed class NotificationHistoryAdminService(
    INotificationsRepository repository,
    NotificationDispatcher dispatcher) : INotificationHistoryAdminService
{
    public async Task<Result<IReadOnlyList<NotificationLogSummaryDto>>> ListAsync(
        int? storeId,
        NotificationDeliveryStatus? status,
        int? customerId,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var logs = await repository.ListLogsAsync(storeId, status, customerId, Math.Clamp(take, 1, 500), cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<NotificationLogSummaryDto>>(logs.Select(MapLog).ToList());
    }

    public async Task<Result> RetryAsync(int logId, CancellationToken cancellationToken = default)
    {
        var log = await repository.GetLogByIdAsync(logId, cancellationToken).ConfigureAwait(false);
        if (log is null)
        {
            return Result.Failure(Error.NotFound("Notification log not found."));
        }

        await dispatcher.RetryLogAsync(log, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static NotificationLogSummaryDto MapLog(NotificationLog log) =>
        new(
            log.Id,
            log.EventType,
            log.Channel,
            log.StoreId,
            log.CustomerId,
            MaskRecipient(log.Recipient, log.Channel),
            log.Subject,
            log.Status,
            log.AttemptCount,
            log.CreatedAtUtc,
            log.SentAtUtc,
            log.LastError);

    private static string MaskRecipient(string recipient, NotificationChannel channel)
    {
        if (channel is NotificationChannel.InApp)
        {
            return recipient;
        }

        if (recipient.Contains('@'))
        {
            var parts = recipient.Split('@');
            return parts[0].Length <= 2 ? $"**@{parts[1]}" : $"{parts[0][..2]}***@{parts[1]}";
        }

        return recipient.Length <= 4 ? "****" : $"{recipient[..2]}***{recipient[^2..]}";
    }
}

public sealed class InAppNotificationStorefrontService(INotificationsRepository repository) : IInAppNotificationStorefrontService
{
    public async Task<IReadOnlyList<InAppNotificationDto>> ListUnreadAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.ListUnreadInAppAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        return items.Select(x => new InAppNotificationDto(x.Id, x.Title, x.Body, x.IsRead, x.CreatedAtUtc, x.ReadAtUtc)).ToList();
    }

    public async Task MarkReadAsync(int customerId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await repository.GetInAppByIdAsync(notificationId, cancellationToken).ConfigureAwait(false);
        if (notification is null || notification.CustomerId != customerId)
        {
            return;
        }

        notification.MarkRead();
        await repository.SaveInAppNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
