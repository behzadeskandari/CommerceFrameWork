using Commerce.Framework.Contracts.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class DistributedCacheManager(
    IDistributedCache distributedCache,
    CacheKeyRegistry registry,
    ILogger<DistributedCacheManager> logger) : ICacheManager
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        CacheGuard.EnsureSafeKey(key);
        var payload = await distributedCache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (payload is null || payload.Length == 0)
        {
            return null;
        }

        try
        {
            return CacheSerialization.Deserialize<T>(payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Distributed cache deserialize failed for {CacheKey}", key);
            await distributedCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        CacheGuard.EnsureSafeKey(key);
        var entryOptions = options ?? new CacheEntryOptions();
        var payload = CacheSerialization.Serialize(value);
        await distributedCache.SetAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = entryOptions.AbsoluteExpiration
            },
            cancellationToken).ConfigureAwait(false);

        registry.Track(key, entryOptions.Tags, ExtractPrefix(key));
        logger.LogDebug("Distributed cache set {CacheKey}", key);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        registry.Untrack(key, [], ExtractPrefix(key));
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        foreach (var key in registry.GetKeysForTag(tag))
        {
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in registry.GetKeysForPrefix(prefix))
        {
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken).ConfigureAwait(false);
        await SetAsync(key, value, options, cancellationToken).ConfigureAwait(false);
        return value;
    }

    private static string ExtractPrefix(string key)
    {
        var lastSeparator = key.LastIndexOf(':');
        return lastSeparator < 0 ? key : key[..(lastSeparator + 1)];
    }
}
