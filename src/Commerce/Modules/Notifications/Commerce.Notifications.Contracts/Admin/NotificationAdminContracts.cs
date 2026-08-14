using Commerce.Framework.Core.Results;
using Commerce.Notifications.Domain.Enums;

namespace Commerce.Notifications.Contracts.Admin;

public sealed record NotificationTemplateSummaryDto(
    int Id,
    string SystemName,
    NotificationEventType EventType,
    NotificationChannel Channel,
    int? LanguageId,
    int? StoreId,
    bool IsEnabled);

public sealed record NotificationTemplateDetailDto(
    int Id,
    string SystemName,
    NotificationEventType EventType,
    NotificationChannel Channel,
    string Subject,
    string Body,
    int? LanguageId,
    int? StoreId,
    string? VariablesJson,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record NotificationLogSummaryDto(
    int Id,
    NotificationEventType EventType,
    NotificationChannel Channel,
    int? StoreId,
    int? CustomerId,
    string Recipient,
    string Subject,
    NotificationDeliveryStatus Status,
    int AttemptCount,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    string? LastError);

public sealed record CreateNotificationTemplateRequest(
    string SystemName,
    NotificationEventType EventType,
    NotificationChannel Channel,
    string Subject,
    string Body,
    int? LanguageId,
    int? StoreId,
    string? VariablesJson,
    bool IsEnabled);

public sealed record UpdateNotificationTemplateRequest(
    string Subject,
    string Body,
    int? LanguageId,
    int? StoreId,
    string? VariablesJson,
    bool IsEnabled);

public interface INotificationTemplateAdminService
{
    Task<Result<IReadOnlyList<NotificationTemplateSummaryDto>>> ListAsync(
        int? storeId,
        NotificationEventType? eventType,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationTemplateDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<NotificationTemplateDetailDto>> CreateAsync(
        CreateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationTemplateDetailDto>> UpdateAsync(
        int id,
        UpdateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public interface INotificationHistoryAdminService
{
    Task<Result<IReadOnlyList<NotificationLogSummaryDto>>> ListAsync(
        int? storeId,
        NotificationDeliveryStatus? status,
        int? customerId,
        int take = 100,
        CancellationToken cancellationToken = default);

    Task<Result> RetryAsync(int logId, CancellationToken cancellationToken = default);
}
