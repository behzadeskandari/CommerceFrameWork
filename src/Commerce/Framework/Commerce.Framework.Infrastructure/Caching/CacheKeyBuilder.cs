using Commerce.Framework.Contracts.Caching;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class CacheKeyBuilder(IOptions<CacheOptions> options) : ICacheKeyBuilder
{
    public string ProductList(string? term) =>
        Build(CacheCategories.Products, "list", Normalize(term));

    public string ProductDetail(int productId) =>
        Build(CacheCategories.Products, "detail", productId.ToString());

    public string ProductBySlug(string slug) =>
        Build(CacheCategories.Products, "slug", Normalize(slug));

    public string Setting(string key, int? storeId) =>
        Build(CacheCategories.Settings, Normalize(key), (storeId ?? 0).ToString());

    public string SearchQuery(string fingerprint) =>
        Build(CacheCategories.Search, "query", fingerprint);

    public string SearchSuggest(string term, int storeId, int languageId) =>
        Build(CacheCategories.Search, "suggest", storeId.ToString(), languageId.ToString(), Normalize(term));

    public string Prefix(string category) => $"{options.Value.KeyPrefix}:{category}:";

    private string Build(string category, params string[] parts) =>
        $"{options.Value.KeyPrefix}:{category}:{string.Join(':', parts)}";

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "_" : value.Trim().ToLowerInvariant();
}
