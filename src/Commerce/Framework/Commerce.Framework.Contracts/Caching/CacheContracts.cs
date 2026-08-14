namespace Commerce.Framework.Contracts.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Commerce:Cache";

    public bool Enabled { get; set; } = true;

    public string Provider { get; set; } = "Memory";

    public string KeyPrefix { get; set; } = "commerce";

    public string? RedisConnectionString { get; set; }

    public CachePolicyOptions Products { get; set; } = new() { TtlMinutes = 15 };

    public CachePolicyOptions Search { get; set; } = new() { TtlMinutes = 2 };

    public CachePolicyOptions Settings { get; set; } = new() { TtlMinutes = 60 };

    public CachePolicyOptions Output { get; set; } = new() { TtlMinutes = 2 };
}

public sealed class CachePolicyOptions
{
    public int TtlMinutes { get; set; } = 15;

    public TimeSpan Ttl => TimeSpan.FromMinutes(TtlMinutes);
}

public sealed class CacheEntryOptions
{
    public TimeSpan AbsoluteExpiration { get; init; } = TimeSpan.FromMinutes(15);

    public IReadOnlyList<string> Tags { get; init; } = [];
}

public static class CacheCategories
{
    public const string Products = "products";
    public const string Search = "search";
    public const string Settings = "settings";
    public const string Configuration = "configuration";
}

public static class CacheDeniedSegments
{
    public static readonly string[] Segments =
    [
        "cart",
        "checkout",
        "payment",
        "order",
        "inventory",
        "giftcard",
        "wallet",
        "reservation"
    ];
}

public interface ICacheManager
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class;
}

public interface ICacheKeyBuilder
{
    string ProductList(string? term);

    string ProductDetail(int productId);

    string ProductBySlug(string slug);

    string Setting(string key, int? storeId);

    string SearchQuery(string fingerprint);

    string SearchSuggest(string term, int storeId, int languageId);

    string Prefix(string category);
}

public interface ICacheInvalidator
{
    Task InvalidateProductAsync(int productId, CancellationToken cancellationToken = default);

    Task InvalidateAllProductsAsync(CancellationToken cancellationToken = default);

    Task InvalidateSearchAsync(CancellationToken cancellationToken = default);

    Task InvalidateSettingAsync(string key, int? storeId, CancellationToken cancellationToken = default);
}

public interface IDistributedLockHandle : IAsyncDisposable
{
    bool IsAcquired { get; }
}

public interface IDistributedLockProvider
{
    Task<IDistributedLockHandle> AcquireAsync(
        string resource,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
