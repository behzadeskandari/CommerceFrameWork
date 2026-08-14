using Commerce.Integration.Application.Abstractions;
using Commerce.Integration.Application.Webhooks;
using Commerce.Integration.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Integration.Infrastructure.Persistence.Repositories;

public sealed class EfIntegrationRepository(CommerceDbContext dbContext) : IIntegrationRepository
{
    public Task<WebhookSubscription?> GetSubscriptionByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<WebhookSubscription>> ListSubscriptionsAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<WebhookSubscription>().AsNoTracking().Where(x => !x.IsDeleted);
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(
        string eventType,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await dbContext.Set<WebhookSubscription>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => !x.StoreId.HasValue || !storeId.HasValue || x.StoreId == storeId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return subscriptions.Where(x => x.SubscribesTo(eventType)).ToList();
    }

    public async Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        dbContext.Set<WebhookSubscription>().Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateSubscriptionAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        dbContext.Set<WebhookSubscription>().Update(subscription);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<WebhookDelivery?> GetDeliveryByIdempotencyKeyAsync(
        int subscriptionId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<WebhookDelivery>()
            .FirstOrDefaultAsync(
                x => x.WebhookSubscriptionId == subscriptionId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public async Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        dbContext.Set<WebhookDelivery>().Add(delivery);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        dbContext.Set<WebhookDelivery>().Update(delivery);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WebhookDelivery>> ListDeliveriesForSubscriptionAsync(
        int subscriptionId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<WebhookDelivery>()
            .AsNoTracking()
            .Where(x => x.WebhookSubscriptionId == subscriptionId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<WebhookDelivery>> GetPendingDeliveriesAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<WebhookDelivery>()
            .Where(x =>
                (x.Status == Domain.Enums.WebhookDeliveryStatus.Pending ||
                 x.Status == Domain.Enums.WebhookDeliveryStatus.Failed) &&
                x.AttemptCount < WebhookDeliveryProcessor.MaxAttempts &&
                (!x.NextRetryAtUtc.HasValue || x.NextRetryAtUtc.Value <= utcNow))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<ApiClient?> GetApiClientByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default) =>
        dbContext.Set<ApiClient>()
            .FirstOrDefaultAsync(x => x.KeyPrefix == keyPrefix && !x.IsDeleted, cancellationToken);

    public Task<ApiClient?> GetApiClientByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ApiClient>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<ApiClient>> ListApiClientsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ApiClient>().AsNoTracking().Where(x => !x.IsDeleted);
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddApiClientAsync(ApiClient client, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ApiClient>().Add(client);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateApiClientAsync(ApiClient client, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ApiClient>().Update(client);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryRecordProcessedEventAsync(
        Guid integrationEventId,
        string eventType,
        string consumerKey,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<ProcessedIntegrationEvent>()
            .AnyAsync(
                x => x.IntegrationEventId == integrationEventId && x.ConsumerKey == consumerKey,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing)
        {
            return false;
        }

        dbContext.Set<ProcessedIntegrationEvent>().Add(
            ProcessedIntegrationEvent.Record(integrationEventId, eventType, consumerKey));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
