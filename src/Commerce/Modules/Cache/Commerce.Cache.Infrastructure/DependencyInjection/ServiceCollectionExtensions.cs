using Commerce.Cache.Application;
using Commerce.Catalog.Application.Storefront;
using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Contracts.Caching;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Infrastructure.Caching;
using Commerce.Search.Application;
using Commerce.Search.Contracts;
using Commerce.Store.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Commerce.Cache.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCacheInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<CacheKeyRegistry>();
        services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
        services.AddSingleton<ICacheInvalidator, CacheInvalidator>();

        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        RegisterCacheBackend(services, cacheOptions);
        RegisterDistributedLock(services, cacheOptions);

        services.AddScoped<CacheCatalogInvalidator>();
        services.AddScoped<ICatalogChangeNotifier>(sp => sp.GetRequiredService<CacheCatalogInvalidator>());

        services.AddScoped<StorefrontCatalogService>();
        services.Replace(ServiceDescriptor.Scoped<IStorefrontCatalogService>(sp =>
            new CachedStorefrontCatalogService(
                sp.GetRequiredService<StorefrontCatalogService>(),
                sp.GetRequiredService<ICacheManager>(),
                sp.GetRequiredService<ICacheKeyBuilder>(),
                sp.GetRequiredService<IOptions<CacheOptions>>())));

        services.AddScoped<SettingService>();
        services.Replace(ServiceDescriptor.Scoped<ISettingService>(sp =>
            new CachedSettingService(
                sp.GetRequiredService<SettingService>(),
                sp.GetRequiredService<ICacheManager>(),
                sp.GetRequiredService<ICacheKeyBuilder>(),
                sp.GetRequiredService<ICacheInvalidator>(),
                sp.GetRequiredService<IOptions<CacheOptions>>())));

        services.AddScoped<SearchQueryService>();
        services.Replace(ServiceDescriptor.Scoped<ISearchQueryService>(sp =>
            new CachedSearchQueryService(
                sp.GetRequiredService<SearchQueryService>(),
                sp.GetRequiredService<ICacheManager>(),
                sp.GetRequiredService<ICacheKeyBuilder>(),
                sp.GetRequiredService<IOptions<CacheOptions>>())));

        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.NoCache());
            options.AddPolicy(CacheOutputPolicies.StorefrontCatalog, builder => builder
                .Expire(TimeSpan.FromMinutes(cacheOptions.Output.TtlMinutes))
                .SetVaryByQuery("term")
                .Tag(CacheCategories.Products));
            options.AddPolicy(CacheOutputPolicies.StorefrontSearch, builder => builder
                .Expire(TimeSpan.FromMinutes(cacheOptions.Output.TtlMinutes))
                .SetVaryByQuery("q", "term", "page", "pageSize", "sortField", "sortDirection", "categoryId")
                .Tag(CacheCategories.Search));
        });

        return services;
    }

    private static void RegisterCacheBackend(IServiceCollection services, CacheOptions cacheOptions)
    {
        if (!cacheOptions.Enabled)
        {
            services.TryAddSingleton<ICacheManager, NullCacheManager>();
            return;
        }

        services.AddSingleton<MemoryCacheManager>();

        if (IsRedisProvider(cacheOptions))
        {
            services.AddSingleton<DistributedCacheManager>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnectionString;
                options.InstanceName = cacheOptions.KeyPrefix + ":";
            });

            services.TryAddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(cacheOptions.RedisConnectionString!));

            services.TryAddSingleton<ICacheManager>(sp => new CompositeCacheManager(
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetRequiredService<DistributedCacheManager>(),
                sp.GetRequiredService<CacheKeyRegistry>(),
                sp.GetRequiredService<IDistributedLockProvider>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CompositeCacheManager>>()));
            return;
        }

        services.TryAddSingleton<ICacheManager>(sp => sp.GetRequiredService<MemoryCacheManager>());
    }

    private static void RegisterDistributedLock(IServiceCollection services, CacheOptions cacheOptions)
    {
        if (IsRedisProvider(cacheOptions))
        {
            services.TryAddSingleton<IDistributedLockProvider, Locking.RedisDistributedLockProvider>();
            return;
        }

        services.TryAddSingleton<IDistributedLockProvider, InMemoryDistributedLockProvider>();
    }

    private static bool IsRedisProvider(CacheOptions cacheOptions) =>
        cacheOptions.Enabled &&
        cacheOptions.Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(cacheOptions.RedisConnectionString);
}

public static class CacheOutputPolicies
{
    public const string StorefrontCatalog = "commerce.storefront.catalog";
    public const string StorefrontSearch = "commerce.storefront.search";
}

public static class CacheApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCommerceOutputCache(this IApplicationBuilder app) =>
        app.UseOutputCache();
}
