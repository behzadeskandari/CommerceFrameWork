using System.Security.Cryptography;
using System.Text;
using Commerce.Catalog.Application.Storefront;
using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Contracts.Caching;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Search;
using Commerce.Search.Application;
using Commerce.Search.Contracts;
using Commerce.Store.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Commerce.Cache.Application;

public sealed class CachedStorefrontCatalogService(
    StorefrontCatalogService inner,
    ICacheManager cacheManager,
    ICacheKeyBuilder keyBuilder,
    IOptions<CacheOptions> cacheOptions) : IStorefrontCatalogService
{
    public async Task<Result<IReadOnlyList<ProductSummaryDto>>> ListProductsAsync(
        string? term = null,
        CancellationToken cancellationToken = default)
    {
        var key = keyBuilder.ProductList(term);
        var options = BuildOptions(
            cacheOptions.Value.Products.Ttl,
            [CacheCategories.Products, $"{CacheCategories.Products}:list"]);

        var cached = await cacheManager.GetAsync<CachedResultEnvelope<IReadOnlyList<ProductSummaryDto>>>(key, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            return cached.ToResult();
        }

        var result = await inner.ListProductsAsync(term, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await cacheManager.SetAsync(key, CachedResultEnvelope<IReadOnlyList<ProductSummaryDto>>.From(result), options, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<Result<StorefrontProductDetailDto>> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var key = keyBuilder.ProductDetail(productId);
        var options = BuildOptions(
            cacheOptions.Value.Products.Ttl,
            [CacheCategories.Products, $"{CacheCategories.Products}:product:{productId}"]);

        var cached = await cacheManager.GetAsync<CachedResultEnvelope<StorefrontProductDetailDto>>(key, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            return cached.ToResult();
        }

        var result = await inner.GetProductByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await cacheManager.SetAsync(key, CachedResultEnvelope<StorefrontProductDetailDto>.From(result), options, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<Result<StorefrontProductDetailDto>> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var key = keyBuilder.ProductBySlug(slug);
        var options = BuildOptions(
            cacheOptions.Value.Products.Ttl,
            [CacheCategories.Products, $"{CacheCategories.Products}:slug"]);

        var cached = await cacheManager.GetAsync<CachedResultEnvelope<StorefrontProductDetailDto>>(key, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            return cached.ToResult();
        }

        var result = await inner.GetProductBySlugAsync(slug, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await cacheManager.SetAsync(key, CachedResultEnvelope<StorefrontProductDetailDto>.From(result), options, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                var detailKey = keyBuilder.ProductDetail(result.Value.Id);
                await cacheManager.SetAsync(detailKey, CachedResultEnvelope<StorefrontProductDetailDto>.From(result), options, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    private static CacheEntryOptions BuildOptions(TimeSpan ttl, IReadOnlyList<string> tags) =>
        new() { AbsoluteExpiration = ttl, Tags = tags };
}

public sealed class CachedSettingService(
    SettingService inner,
    ICacheManager cacheManager,
    ICacheKeyBuilder keyBuilder,
    ICacheInvalidator cacheInvalidator,
    IOptions<CacheOptions> cacheOptions) : ISettingService
{
    public async Task<string?> GetRawAsync(
        string key,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = keyBuilder.Setting(key, storeId);
        var options = new CacheEntryOptions
        {
            AbsoluteExpiration = cacheOptions.Value.Settings.Ttl,
            Tags = [CacheCategories.Settings, CacheCategories.Configuration]
        };

        var envelope = await cacheManager.GetOrCreateAsync(
            cacheKey,
            async ct => new CachedSettingEnvelope(await inner.GetRawAsync(key, storeId, ct).ConfigureAwait(false)),
            options,
            cancellationToken).ConfigureAwait(false);

        return envelope.Value;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var raw = await GetRawAsync(key, storeId, cancellationToken).ConfigureAwait(false);
        if (raw is null)
        {
            return default;
        }

        return ConvertSettingValue<T>(raw);
    }

    private static T? ConvertSettingValue<T>(string raw)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType == typeof(string))
        {
            return (T?)(object)raw;
        }

        if (targetType == typeof(bool))
        {
            return (T?)(object)bool.Parse(raw);
        }

        if (targetType == typeof(int))
        {
            return (T?)(object)int.Parse(raw);
        }

        if (targetType == typeof(decimal))
        {
            return (T?)(object)decimal.Parse(raw);
        }

        if (targetType == typeof(DateTime))
        {
            return (T?)(object)DateTime.Parse(raw);
        }

        return (T?)Convert.ChangeType(raw, targetType);
    }

    public async Task SetAsync(
        string key,
        string value,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await inner.SetAsync(key, value, storeId, cancellationToken).ConfigureAwait(false);
        await cacheInvalidator.InvalidateSettingAsync(key, storeId, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<SettingEntryDto>> ListAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default) =>
        inner.ListAsync(storeId, cancellationToken);
}

public sealed class CachedSearchQueryService(
    SearchQueryService inner,
    ICacheManager cacheManager,
    ICacheKeyBuilder keyBuilder,
    IOptions<CacheOptions> cacheOptions) : ISearchQueryService
{
    public async Task<SearchQueryResult> SearchAsync(
        SearchQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var fingerprint = SearchRequestFingerprint.ForQuery(request);
        var cacheKey = keyBuilder.SearchQuery(fingerprint);
        var options = new CacheEntryOptions
        {
            AbsoluteExpiration = cacheOptions.Value.Search.Ttl,
            Tags = [CacheCategories.Search, $"{CacheCategories.Search}:query"]
        };

        return await cacheManager.GetOrCreateAsync(
            cacheKey,
            ct => inner.SearchAsync(request, ct),
            options,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<SearchSuggestionResult> SuggestAsync(
        SearchSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = keyBuilder.SearchSuggest(request.Term, request.StoreId, request.LanguageId);
        var options = new CacheEntryOptions
        {
            AbsoluteExpiration = cacheOptions.Value.Search.Ttl,
            Tags = [CacheCategories.Search, $"{CacheCategories.Search}:suggest"]
        };

        return await cacheManager.GetOrCreateAsync(
            cacheKey,
            ct => inner.SuggestAsync(request, ct),
            options,
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class CacheCatalogInvalidator(ICacheInvalidator cacheInvalidator) : ICatalogChangeNotifier
{
    public async Task NotifyProductCreatedAsync(int productId, CancellationToken cancellationToken = default) =>
        await InvalidateAsync(productId, cancellationToken).ConfigureAwait(false);

    public async Task NotifyProductUpdatedAsync(int productId, CancellationToken cancellationToken = default) =>
        await InvalidateAsync(productId, cancellationToken).ConfigureAwait(false);

    public async Task NotifyProductDeletedAsync(int productId, CancellationToken cancellationToken = default) =>
        await InvalidateAsync(productId, cancellationToken).ConfigureAwait(false);

    private async Task InvalidateAsync(int productId, CancellationToken cancellationToken)
    {
        await cacheInvalidator.InvalidateProductAsync(productId, cancellationToken).ConfigureAwait(false);
        await cacheInvalidator.InvalidateSearchAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class CachedResultEnvelope<T>
{
    public bool IsSuccess { get; init; }

    public T? Value { get; init; }

    public Commerce.Framework.Core.Errors.ErrorCode? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static CachedResultEnvelope<T> From(Result<T> result) =>
        result.IsSuccess
            ? new CachedResultEnvelope<T> { IsSuccess = true, Value = result.Value }
            : new CachedResultEnvelope<T>
            {
                IsSuccess = false,
                ErrorCode = result.Error?.Code,
                ErrorMessage = result.Error?.Message
            };

    public Result<T> ToResult()
    {
        if (IsSuccess)
        {
            return Result.Success(Value!);
        }

        return Result.Failure<T>(Commerce.Framework.Core.Errors.Error.Failure(
            ErrorMessage ?? "Cached failure.",
            ErrorCode ?? Commerce.Framework.Core.Errors.ErrorCode.OperationFailed));
    }
}

internal sealed class CachedSettingEnvelope
{
    public string? Value { get; init; }

    public CachedSettingEnvelope()
    {
    }

    public CachedSettingEnvelope(string? value) => Value = value;
}

public static class SearchRequestFingerprint
{
    public static string ForQuery(SearchQueryRequest request)
    {
        var builder = new StringBuilder(256);
        builder.Append(request.StoreId).Append('|')
            .Append(request.LanguageId).Append('|')
            .Append(request.Page).Append('|')
            .Append(request.PageSize).Append('|')
            .Append(request.SortField).Append('|')
            .Append(request.SortDirection).Append('|')
            .Append(request.Term ?? string.Empty).Append('|')
            .Append(request.CategoryId).Append('|')
            .Append(request.Manufacturer ?? string.Empty).Append('|')
            .Append(request.MinPrice).Append('|')
            .Append(request.MaxPrice).Append('|')
            .Append(request.ProductType ?? string.Empty).Append('|')
            .Append(request.IsAvailable);

        if (request.Attributes is not null)
        {
            foreach (var attribute in request.Attributes.OrderBy(x => x.Code, StringComparer.Ordinal))
            {
                builder.Append('|').Append(attribute.Code).Append('=').Append(attribute.Value);
            }
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash);
    }
}

public sealed class CachePerformanceMeasurement
{
    public required string Operation { get; init; }

    public required long UncachedElapsedMs { get; init; }

    public required long CachedElapsedMs { get; init; }

    public double SpeedupFactor =>
        CachedElapsedMs <= 0 ? UncachedElapsedMs : Math.Round((double)UncachedElapsedMs / CachedElapsedMs, 2);
}

public static class CachePerformanceProfiler
{
    public static async Task<CachePerformanceMeasurement> MeasureAsync(
        string operation,
        Func<CancellationToken, Task> uncached,
        Func<CancellationToken, Task> cached,
        CancellationToken cancellationToken = default)
    {
        var uncachedMs = await MeasureAsync(uncached, cancellationToken).ConfigureAwait(false);
        var cachedMs = await MeasureAsync(cached, cancellationToken).ConfigureAwait(false);
        return new CachePerformanceMeasurement
        {
            Operation = operation,
            UncachedElapsedMs = uncachedMs,
            CachedElapsedMs = cachedMs
        };
    }

    private static async Task<long> MeasureAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var started = Environment.TickCount64;
        await action(cancellationToken).ConfigureAwait(false);
        return Math.Max(0, Environment.TickCount64 - started);
    }
}
