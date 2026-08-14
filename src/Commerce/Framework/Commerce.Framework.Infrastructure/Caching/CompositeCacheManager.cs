using Commerce.Framework.Contracts.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class CompositeCacheManager(
    IMemoryCache memoryCache,
    ICacheManager distributedCacheManager,
    CacheKeyRegistry registry,
    IDistributedLockProvider lockProvider,
    ILogger<CompositeCacheManager> logger) : ICacheManager
{
    private static readonly TimeSpan MemoryLayerCap = TimeSpan.FromMinutes(1);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        CacheGuard.EnsureSafeKey(key);
        cancellationToken.ThrowIfCancellationRequested();

        if (memoryCache.TryGetValue(key, out T? local) && local is not null)
        {
            return local;
        }

        var remote = await distributedCacheManager.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (remote is not null)
        {
            memoryCache.Set(key, remote, MemoryLayerCap);
        }

        return remote;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var entryOptions = options ?? new CacheEntryOptions();
        memoryCache.Set(key, value, Min(entryOptions.AbsoluteExpiration, MemoryLayerCap));
        await distributedCacheManager.SetAsync(key, value, entryOptions, cancellationToken).ConfigureAwait(false);
        registry.Track(key, entryOptions.Tags, ExtractPrefix(key));
        logger.LogDebug("Composite cache set {CacheKey}", key);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        await distributedCacheManager.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        registry.Untrack(key, [], ExtractPrefix(key));
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        distributedCacheManager.RemoveByTagAsync(tag, cancellationToken);

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        distributedCacheManager.RemoveByPrefixAsync(prefix, cancellationToken);

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

        await using var handle = await lockProvider.AcquireAsync($"cache-fill:{key}", TimeSpan.FromSeconds(10), cancellationToken)
            .ConfigureAwait(false);
        if (!handle.IsAcquired)
        {
            logger.LogWarning("Cache fill lock not acquired for {CacheKey}; bypassing lock.", key);
        }

        cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken).ConfigureAwait(false);
        await SetAsync(key, value, options, cancellationToken).ConfigureAwait(false);
        return value;
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right) =>
        left <= right ? left : right;

    private static string ExtractPrefix(string key)
    {
        var lastSeparator = key.LastIndexOf(':');
        return lastSeparator < 0 ? key : key[..(lastSeparator + 1)];
    }
}
