using System.Security.Cryptography;
using System.Text.Json;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Events;
using Commerce.Integration.Application.Abstractions;
using Commerce.Integration.Contracts.Events;
using Commerce.Integration.Contracts.Webhooks;
using Commerce.Integration.Domain.Entities;
using Commerce.Integration.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Integration.Application.Webhooks;

internal static class IntegrationMapper
{
    internal static WebhookSubscriptionSummaryDto MapSummary(WebhookSubscription subscription) =>
        new(
            subscription.Id,
            subscription.StoreId,
            subscription.Name,
            subscription.Url,
            subscription.EventTypes.ToList(),
            subscription.IsActive,
            subscription.CreatedAtUtc);

    internal static WebhookSubscriptionDetailDto MapDetail(WebhookSubscription subscription) =>
        new(
            subscription.Id,
            subscription.StoreId,
            subscription.Name,
            subscription.Url,
            subscription.EventTypes.ToList(),
            subscription.IsActive,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);

    internal static WebhookDeliveryDto MapDelivery(WebhookDelivery delivery) =>
        new(
            delivery.Id,
            delivery.WebhookSubscriptionId,
            delivery.IntegrationEventId,
            delivery.EventType,
            delivery.Status,
            delivery.AttemptCount,
            delivery.NextRetryAtUtc,
            delivery.ResponseStatusCode,
            delivery.ErrorMessage,
            delivery.CreatedAtUtc,
            delivery.UpdatedAtUtc);
}

public sealed class IntegrationEventPublisher(IEventBus eventBus) : IIntegrationEventPublisher
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default) =>
        eventBus.PublishAsync(integrationEvent, cancellationToken);
}

public sealed class IntegrationEventIdempotencyService(IIntegrationRepository repository) : IIntegrationEventIdempotencyService
{
    public Task<bool> TryMarkProcessedAsync(
        Guid integrationEventId,
        string eventType,
        string consumerKey,
        CancellationToken cancellationToken = default) =>
        repository.TryRecordProcessedEventAsync(integrationEventId, eventType, consumerKey, cancellationToken);
}

public sealed class WebhookDispatchIntegrationHandler(
    IIntegrationRepository repository,
    ILogger<WebhookDispatchIntegrationHandler> logger) : IIntegrationEventHandler
{
    public IReadOnlyCollection<string> SupportedEventTypes { get; } =
    [
        IntegrationEventTypes.OrderCreated,
        IntegrationEventTypes.OrderPaid,
        IntegrationEventTypes.OrderCancelled,
        IntegrationEventTypes.PaymentSucceeded,
        IntegrationEventTypes.PaymentFailed,
        IntegrationEventTypes.ProductCreated,
        IntegrationEventTypes.ProductUpdated,
        IntegrationEventTypes.CustomerRegistered,
        IntegrationEventTypes.InventoryChanged,
        IntegrationEventTypes.ShipmentCreated,
        IntegrationEventTypes.RefundCreated
    ];

    public async Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var subscriptions = await repository
            .GetActiveSubscriptionsForEventAsync(integrationEvent.EventType, integrationEvent.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (subscriptions.Count == 0)
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), IntegrationJson.Options);

        foreach (var subscription in subscriptions)
        {
            var idempotencyKey = $"{subscription.Id}:{integrationEvent.EventId:N}";
            var existing = await repository
                .GetDeliveryByIdempotencyKeyAsync(subscription.Id, idempotencyKey, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                logger.LogDebug(
                    "Skipping duplicate webhook delivery for subscription {SubscriptionId} event {EventId}.",
                    subscription.Id,
                    integrationEvent.EventId);
                continue;
            }

            var delivery = WebhookDelivery.Create(
                subscription.Id,
                integrationEvent.EventId,
                integrationEvent.EventType,
                payloadJson,
                idempotencyKey);

            await repository.AddDeliveryAsync(delivery, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class WebhookAdminService(IIntegrationRepository repository) : IWebhookAdminService
{
    public async Task<Result<IReadOnlyList<WebhookSubscriptionSummaryDto>>> ListSubscriptionsAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.ListSubscriptionsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WebhookSubscriptionSummaryDto>>(
            items.Select(IntegrationMapper.MapSummary).ToList());
    }

    public async Task<Result<WebhookSubscriptionDetailDto>> GetSubscriptionAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetSubscriptionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return subscription is null
            ? Result.Failure<WebhookSubscriptionDetailDto>(Error.NotFound("Webhook subscription not found."))
            : Result.Success(IntegrationMapper.MapDetail(subscription));
    }

    public async Task<Result<(WebhookSubscriptionDetailDto Subscription, string Secret)>> CreateSubscriptionAsync(
        CreateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var secret = GenerateSecret();
            var subscription = WebhookSubscription.Create(
                request.StoreId,
                request.Name,
                request.Url,
                secret,
                request.EventTypes,
                request.IsActive);

            await repository.AddSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
            return Result.Success((IntegrationMapper.MapDetail(subscription), secret));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<(WebhookSubscriptionDetailDto, string)>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<WebhookSubscriptionDetailDto>> UpdateSubscriptionAsync(
        int id,
        UpdateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await repository.GetSubscriptionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result.Failure<WebhookSubscriptionDetailDto>(Error.NotFound("Webhook subscription not found."));
        }

        try
        {
            subscription.Update(request.Name, request.Url, request.EventTypes, request.IsActive);
            await repository.UpdateSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
            return Result.Success(IntegrationMapper.MapDetail(subscription));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<WebhookSubscriptionDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<string>> RotateSecretAsync(int id, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetSubscriptionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result.Failure<string>(Error.NotFound("Webhook subscription not found."));
        }

        var secret = GenerateSecret();
        subscription.RotateSecret(secret);
        await repository.UpdateSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
        return Result.Success(secret);
    }

    public async Task<Result> DeleteSubscriptionAsync(int id, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetSubscriptionByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result.Failure(Error.NotFound("Webhook subscription not found."));
        }

        subscription.SoftDelete();
        await repository.UpdateSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<WebhookDeliveryDto>>> ListDeliveriesAsync(
        int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var deliveries = await repository
            .ListDeliveriesForSubscriptionAsync(subscriptionId, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WebhookDeliveryDto>>(
            deliveries.Select(IntegrationMapper.MapDelivery).ToList());
    }

    private static string GenerateSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}

internal static class IntegrationJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
