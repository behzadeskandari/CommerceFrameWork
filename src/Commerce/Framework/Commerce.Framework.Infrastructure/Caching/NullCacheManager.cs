using Commerce.Framework.Contracts.Caching;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class NullCacheManager : ICacheManager
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class =>
        Task.FromResult<T?>(null);

    public Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class =>
        Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class =>
        await factory(cancellationToken).ConfigureAwait(false);
}
