using Commerce.Framework.Contracts.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class MemoryCacheManager(
    IMemoryCache memoryCache,
    CacheKeyRegistry registry,
    IOptions<CacheOptions> options,
    ILogger<MemoryCacheManager> logger) : ICacheManager
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        CacheGuard.EnsureSafeKey(key);
        cancellationToken.ThrowIfCancellationRequested();
        memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        CacheGuard.EnsureSafeKey(key);
        cancellationToken.ThrowIfCancellationRequested();

        var entryOptions = options ?? new CacheEntryOptions();
        var prefix = ExtractPrefix(key);
        memoryCache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = entryOptions.AbsoluteExpiration
            });

        registry.Track(key, entryOptions.Tags, prefix);
        logger.LogDebug("Memory cache set {CacheKey}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        memoryCache.Remove(key);
        registry.Untrack(key, [], ExtractPrefix(key));
        return Task.CompletedTask;
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

    private string ExtractPrefix(string key)
    {
        var prefixRoot = options.Value.KeyPrefix + ":";
        if (!key.StartsWith(prefixRoot, StringComparison.Ordinal))
        {
            return key;
        }

        var remainder = key[prefixRoot.Length..];
        var separator = remainder.IndexOf(':');
        return separator < 0
            ? key
            : prefixRoot + remainder[..(separator + 1)];
    }
}
