using Commerce.Framework.Contracts.Caching;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class CacheInvalidator(ICacheManager cacheManager, ICacheKeyBuilder keyBuilder) : ICacheInvalidator
{
    public async Task InvalidateProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        await cacheManager.RemoveAsync(keyBuilder.ProductDetail(productId), cancellationToken).ConfigureAwait(false);
        await cacheManager.RemoveByTagAsync($"{CacheCategories.Products}:product:{productId}", cancellationToken).ConfigureAwait(false);
        await cacheManager.RemoveByPrefixAsync(keyBuilder.Prefix(CacheCategories.Products), cancellationToken).ConfigureAwait(false);
    }

    public Task InvalidateAllProductsAsync(CancellationToken cancellationToken = default) =>
        cacheManager.RemoveByPrefixAsync(keyBuilder.Prefix(CacheCategories.Products), cancellationToken);

    public Task InvalidateSearchAsync(CancellationToken cancellationToken = default) =>
        cacheManager.RemoveByPrefixAsync(keyBuilder.Prefix(CacheCategories.Search), cancellationToken);

    public Task InvalidateSettingAsync(string key, int? storeId, CancellationToken cancellationToken = default) =>
        cacheManager.RemoveAsync(keyBuilder.Setting(key, storeId), cancellationToken);
}
