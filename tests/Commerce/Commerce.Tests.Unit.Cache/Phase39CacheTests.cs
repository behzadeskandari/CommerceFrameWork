using Commerce.Cache.Application;
using Commerce.Framework.Contracts.Caching;
using Commerce.Framework.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Commerce.Tests.Unit.Cache;

public sealed class Phase39CacheTests
{
    private static MemoryCacheManager CreateMemoryCacheManager()
    {
        var options = Options.Create(new CacheOptions { KeyPrefix = "test" });
        return new MemoryCacheManager(
            new MemoryCache(new MemoryCacheOptions()),
            new CacheKeyRegistry(),
            options,
            NullLogger<MemoryCacheManager>.Instance);
    }

    [Fact]
    public void CacheGuard_BlocksDeniedFinancialSegments()
    {
        Assert.Throws<InvalidOperationException>(() => CacheGuard.EnsureSafeKey("commerce:cart:123"));
        Assert.Throws<InvalidOperationException>(() => CacheGuard.EnsureSafeKey("commerce:payment:42"));
    }

    [Fact]
    public async Task MemoryCacheManager_GetSetRemove_Works()
    {
        var cache = CreateMemoryCacheManager();
        await cache.SetAsync("test:products:detail:1", new SamplePayload("alpha"), new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(5),
            Tags = ["products"]
        });

        var cached = await cache.GetAsync<SamplePayload>("test:products:detail:1");
        Assert.NotNull(cached);
        Assert.Equal("alpha", cached!.Name);

        await cache.RemoveAsync("test:products:detail:1");
        Assert.Null(await cache.GetAsync<SamplePayload>("test:products:detail:1"));
    }

    [Fact]
    public async Task CacheInvalidator_RemovesProductEntries()
    {
        var cache = CreateMemoryCacheManager();
        var keyBuilder = new CacheKeyBuilder(Options.Create(new CacheOptions { KeyPrefix = "test" }));
        var invalidator = new CacheInvalidator(cache, keyBuilder);

        await cache.SetAsync(keyBuilder.ProductDetail(10), new SamplePayload("product"), new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(5),
            Tags = [$"{CacheCategories.Products}:product:10"]
        });
        await cache.SetAsync(keyBuilder.ProductList(null), new SamplePayload("list"), new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(5),
            Tags = [CacheCategories.Products]
        });

        await invalidator.InvalidateProductAsync(10);

        Assert.Null(await cache.GetAsync<SamplePayload>(keyBuilder.ProductDetail(10)));
        Assert.Null(await cache.GetAsync<SamplePayload>(keyBuilder.ProductList(null)));
    }

    [Fact]
    public async Task CacheInvalidator_RemovesSearchPrefix()
    {
        var cache = CreateMemoryCacheManager();
        var keyBuilder = new CacheKeyBuilder(Options.Create(new CacheOptions { KeyPrefix = "test" }));
        var invalidator = new CacheInvalidator(cache, keyBuilder);

        await cache.SetAsync(keyBuilder.SearchQuery("abc"), new SamplePayload("search"), new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(5),
            Tags = [CacheCategories.Search]
        });

        await invalidator.InvalidateSearchAsync();

        Assert.Null(await cache.GetAsync<SamplePayload>(keyBuilder.SearchQuery("abc")));
    }

    [Fact]
    public async Task GetOrCreateAsync_ConcurrentAccess_ProducesSingleFactoryExecution()
    {
        var cache = CreateMemoryCacheManager();
        var factoryCalls = 0;

        async Task<SamplePayload> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCalls);
            await Task.Delay(50);
            return new SamplePayload("created");
        }

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => cache.GetOrCreateAsync("test:products:list:_", Factory, new CacheEntryOptions
            {
                AbsoluteExpiration = TimeSpan.FromMinutes(1)
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, result => Assert.Equal("created", result.Name));
        Assert.True(factoryCalls >= 1);
        Assert.True(factoryCalls <= 20);
    }

    [Fact]
    public async Task StaleData_IsRemovedAfterInvalidation()
    {
        var cache = CreateMemoryCacheManager();
        var key = "test:settings:Catalog.ProductsPerPage:0";

        await cache.SetAsync(key, "12", new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(30),
            Tags = [CacheCategories.Settings]
        });

        Assert.Equal("12", await cache.GetAsync<string>(key));
        await cache.RemoveAsync(key);
        Assert.Null(await cache.GetAsync<string>(key));
    }

    [Fact]
    public async Task Failover_NullCacheManager_AlwaysExecutesFactory()
    {
        ICacheManager cache = new NullCacheManager();
        var factoryCalls = 0;

        async Task<SamplePayload> Factory(CancellationToken _)
        {
            factoryCalls++;
            await Task.Delay(1);
            return new SamplePayload("live");
        }

        var first = await cache.GetOrCreateAsync("test:products:list:_", Factory);
        var second = await cache.GetOrCreateAsync("test:products:list:_", Factory);

        Assert.Equal("live", first.Name);
        Assert.Equal("live", second.Name);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task PerformanceMeasurement_ShowsCachedPathIsFaster()
    {
        var cache = CreateMemoryCacheManager();
        var factoryCalls = 0;

        async Task SimulateDatabaseRead(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCalls);
            await Task.Delay(15);
        }

        async Task Uncached(CancellationToken _) => await SimulateDatabaseRead(_);
        async Task Cached(CancellationToken ct)
        {
            await cache.GetOrCreateAsync(
                "test:products:list:_",
                async token =>
                {
                    await SimulateDatabaseRead(token);
                    return new SamplePayload("cached");
                },
                new CacheEntryOptions { AbsoluteExpiration = TimeSpan.FromMinutes(5) },
                ct);
        }

        var measurement = await CachePerformanceProfiler.MeasureAsync("product-list", Uncached, Cached);

        Assert.True(measurement.UncachedElapsedMs > 0);
        Assert.True(measurement.CachedElapsedMs > 0);
        Assert.True(measurement.SpeedupFactor >= 1.0);
    }

    [Fact]
    public void SearchRequestFingerprint_IsStableForSameRequest()
    {
        var request = new Commerce.Framework.Search.SearchQueryRequest(
            "phone",
            StoreId: 1,
            LanguageId: 1,
            Page: 1,
            PageSize: 20);

        var first = SearchRequestFingerprint.ForQuery(request);
        var second = SearchRequestFingerprint.ForQuery(request);

        Assert.Equal(first, second);
    }

    private sealed record SamplePayload(string Name);
}
