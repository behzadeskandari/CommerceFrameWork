using Commerce.Integration.Domain.Entities;

namespace Commerce.Integration.Application.Abstractions;

public interface IIntegrationRepository
{
    Task<WebhookSubscription?> GetSubscriptionByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookSubscription>> ListSubscriptionsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(
        string eventType,
        int? storeId,
        CancellationToken cancellationToken = default);

    Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);

    Task UpdateSubscriptionAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);

    Task<WebhookDelivery?> GetDeliveryByIdempotencyKeyAsync(
        int subscriptionId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);

    Task UpdateDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookDelivery>> ListDeliveriesForSubscriptionAsync(
        int subscriptionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookDelivery>> GetPendingDeliveriesAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default);

    Task<ApiClient?> GetApiClientByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);

    Task<ApiClient?> GetApiClientByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApiClient>> ListApiClientsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddApiClientAsync(ApiClient client, CancellationToken cancellationToken = default);

    Task UpdateApiClientAsync(ApiClient client, CancellationToken cancellationToken = default);

    Task<bool> TryRecordProcessedEventAsync(
        Guid integrationEventId,
        string eventType,
        string consumerKey,
        CancellationToken cancellationToken = default);
}
