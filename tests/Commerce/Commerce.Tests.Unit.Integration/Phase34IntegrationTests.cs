using Commerce.Framework.Core.Entities;
using Commerce.Integration.Application.ApiClients;
using Commerce.Integration.Application.Webhooks;
using Commerce.Integration.Contracts.Events;
using Commerce.Integration.Domain.Entities;
using Commerce.Integration.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Integration;

public sealed class Phase34IntegrationTests
{
    [Fact]
    public void WebhookSignature_ComputeAndVerify_Succeeds()
    {
        var service = new WebhookSignatureService();
        const string secret = "super-secret-key-12345";
        const string payload = """{"eventType":"OrderCreated","orderId":1}""";
        const long timestamp = 1_700_000_000;

        var signature = service.ComputeSignature(secret, timestamp, payload);
        var header = $"t={timestamp},v1={signature}";

        Assert.True(service.VerifySignature(secret, timestamp, payload, header));
    }

    [Fact]
    public void WebhookSignature_InvalidSignature_Fails()
    {
        var service = new WebhookSignatureService();
        const string secret = "super-secret-key-12345";
        const string payload = """{"eventType":"OrderCreated"}""";
        const long timestamp = 1_700_000_000;

        Assert.False(service.VerifySignature(secret, timestamp, payload, "t=1700000000,v1=deadbeef"));
    }

    [Fact]
    public void WebhookDelivery_RetryUntilDeadLetter_AfterMaxAttempts()
    {
        const int maxAttempts = 5;
        var delivery = WebhookDelivery.Create(1, Guid.NewGuid(), "OrderCreated", "{}", "key-1");
        var utcNow = DateTime.UtcNow;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            Assert.True(delivery.CanRetry(utcNow, maxAttempts));
            delivery.MarkDelivering();
            delivery.MarkFailed(500, "HTTP 500", utcNow, maxAttempts);
        }

        Assert.Equal(WebhookDeliveryStatus.DeadLetter, delivery.Status);
        Assert.Equal(maxAttempts, delivery.AttemptCount);
        Assert.False(delivery.CanRetry(utcNow, maxAttempts));
    }

    [Fact]
    public void ApiClientAuthenticator_ValidKey_Succeeds()
    {
        var (prefix, _, fullKey) = ApiClientAdminService.GenerateApiKey();
        var hash = ApiClientAdminService.HashKey(fullKey);
        var client = ApiClient.Create(1, "Partner", prefix, hash, [Commerce.Integration.Contracts.ApiClients.ApiScopes.OrdersRead]);

        var repository = new FakeIntegrationRepository(client);
        var authenticator = new ApiClientAuthenticator(repository);

        var result = authenticator.AuthenticateAsync(fullKey).GetAwaiter().GetResult();

        Assert.True(result.IsAuthenticated);
        Assert.Equal(client.Id, result.ApiClientId);
        Assert.Contains(Commerce.Integration.Contracts.ApiClients.ApiScopes.OrdersRead, result.Scopes);
    }

    [Fact]
    public void ApiClientAuthenticator_InvalidKey_Fails()
    {
        var (prefix, _, fullKey) = ApiClientAdminService.GenerateApiKey();
        var hash = ApiClientAdminService.HashKey(fullKey);
        var client = ApiClient.Create(null, "Partner", prefix, hash, ["orders.read"]);
        var repository = new FakeIntegrationRepository(client);
        var authenticator = new ApiClientAuthenticator(repository);

        var result = authenticator.AuthenticateAsync("ck_invalid_key").GetAwaiter().GetResult();

        Assert.False(result.IsAuthenticated);
    }

    [Fact]
    public async Task IntegrationEventIdempotency_DuplicateEvent_ReturnsFalse()
    {
        var repository = new FakeIntegrationRepository();
        var idempotency = new IntegrationEventIdempotencyService(repository);
        var eventId = Guid.NewGuid();

        var first = await idempotency.TryMarkProcessedAsync(eventId, "OrderCreated", "consumer-a");
        var second = await idempotency.TryMarkProcessedAsync(eventId, "OrderCreated", "consumer-a");

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task WebhookDispatch_DuplicateEvent_DoesNotCreateSecondDelivery()
    {
        var subscription = WebhookSubscription.Create(
            1,
            "Test",
            "https://example.com/hook",
            Convert.ToBase64String(new byte[32]),
            [IntegrationEventTypes.OrderCreated],
            true);

        var repository = new FakeIntegrationRepository(subscription);
        var handler = new WebhookDispatchIntegrationHandler(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<WebhookDispatchIntegrationHandler>.Instance);

        var integrationEvent = new OrderCreatedIntegrationEvent(10, "ORD-10", 5, 99m, "USD")
        {
            StoreId = 1
        };

        await handler.HandleAsync(integrationEvent);
        await handler.HandleAsync(integrationEvent);

        Assert.Single(repository.Deliveries);
    }

    private sealed class FakeIntegrationRepository : Commerce.Integration.Application.Abstractions.IIntegrationRepository
    {
        private readonly Dictionary<string, ProcessedIntegrationEvent> _processed = new();
        private readonly List<WebhookSubscription> _subscriptions;
        public List<WebhookDelivery> Deliveries { get; } = [];

        public FakeIntegrationRepository(params WebhookSubscription[] subscriptions)
        {
            _subscriptions = subscriptions.ToList();
            for (var i = 0; i < _subscriptions.Count; i++)
            {
                typeof(Entity).GetProperty(nameof(Entity.Id))!
                    .SetValue(_subscriptions[i], i + 1);
            }
        }

        public Task<WebhookSubscription?> GetSubscriptionByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_subscriptions.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<WebhookSubscription>> ListSubscriptionsAsync(int? storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebhookSubscription>>(_subscriptions);

        public Task<IReadOnlyList<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(string eventType, int? storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebhookSubscription>>(_subscriptions.Where(x => x.SubscribesTo(eventType)).ToList());

        public Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
        {
            _subscriptions.Add(subscription);
            return Task.CompletedTask;
        }

        public Task UpdateSubscriptionAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<WebhookDelivery?> GetDeliveryByIdempotencyKeyAsync(int subscriptionId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Deliveries.FirstOrDefault(x => x.WebhookSubscriptionId == subscriptionId && x.IdempotencyKey == idempotencyKey));

        public Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
        {
            Deliveries.Add(delivery);
            return Task.CompletedTask;
        }

        public Task UpdateDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<WebhookDelivery>> ListDeliveriesForSubscriptionAsync(int subscriptionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebhookDelivery>>(Deliveries.Where(x => x.WebhookSubscriptionId == subscriptionId).ToList());

        public Task<IReadOnlyList<WebhookDelivery>> GetPendingDeliveriesAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebhookDelivery>>(Deliveries);

        public Task<ApiClient?> GetApiClientByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default) =>
            Task.FromResult(_apiClient?.KeyPrefix == keyPrefix ? _apiClient : null);

        private readonly ApiClient? _apiClient;

        public FakeIntegrationRepository(ApiClient apiClient)
        {
            _apiClient = apiClient;
            _subscriptions = [];
            typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(_apiClient, 1);
        }

        public Task<ApiClient?> GetApiClientByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_apiClient?.Id == id ? _apiClient : null);

        public Task<IReadOnlyList<ApiClient>> ListApiClientsAsync(int? storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ApiClient>>(_apiClient is null ? [] : [_apiClient]);

        public Task AddApiClientAsync(ApiClient client, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateApiClientAsync(ApiClient client, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> TryRecordProcessedEventAsync(Guid integrationEventId, string eventType, string consumerKey, CancellationToken cancellationToken = default)
        {
            var key = $"{integrationEventId:N}:{consumerKey}";
            if (_processed.ContainsKey(key))
            {
                return Task.FromResult(false);
            }

            _processed[key] = ProcessedIntegrationEvent.Record(integrationEventId, eventType, consumerKey);
            return Task.FromResult(true);
        }
    }
}
