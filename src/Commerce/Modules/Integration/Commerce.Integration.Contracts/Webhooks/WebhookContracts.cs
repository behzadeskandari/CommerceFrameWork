using Commerce.Framework.Core.Results;
using Commerce.Integration.Domain.Enums;

namespace Commerce.Integration.Contracts.Webhooks;

public sealed record WebhookSubscriptionSummaryDto(
    int Id,
    int? StoreId,
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record WebhookSubscriptionDetailDto(
    int Id,
    int? StoreId,
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateWebhookSubscriptionRequest(
    int? StoreId,
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes,
    bool IsActive);

public sealed record UpdateWebhookSubscriptionRequest(
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes,
    bool IsActive);

public sealed record WebhookDeliveryDto(
    int Id,
    int WebhookSubscriptionId,
    Guid IntegrationEventId,
    string EventType,
    WebhookDeliveryStatus Status,
    int AttemptCount,
    DateTime? NextRetryAtUtc,
    int? ResponseStatusCode,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public interface IWebhookAdminService
{
    Task<Result<IReadOnlyList<WebhookSubscriptionSummaryDto>>> ListSubscriptionsAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<Result<WebhookSubscriptionDetailDto>> GetSubscriptionAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<(WebhookSubscriptionDetailDto Subscription, string Secret)>> CreateSubscriptionAsync(
        CreateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<WebhookSubscriptionDetailDto>> UpdateSubscriptionAsync(
        int id,
        UpdateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<string>> RotateSecretAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeleteSubscriptionAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<WebhookDeliveryDto>>> ListDeliveriesAsync(
        int subscriptionId,
        CancellationToken cancellationToken = default);
}

public interface IWebhookDeliveryProcessor
{
    Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken = default);
}

public interface IWebhookSignatureService
{
    string ComputeSignature(string secret, long timestampUnix, string payload);

    bool VerifySignature(string secret, long timestampUnix, string payload, string signatureHeader);
}
