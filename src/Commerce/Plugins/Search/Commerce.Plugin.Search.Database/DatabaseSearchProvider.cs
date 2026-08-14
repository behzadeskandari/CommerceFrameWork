using System.Text.Json;
using Commerce.Framework.Search;
using Commerce.Search.Application.Abstractions;
using Commerce.Search.Application.Indexing;
using Commerce.Search.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Search.Database;

public static class DatabaseSearchRegistration
{
    public static IServiceCollection AddDatabaseSearchProvider(this IServiceCollection services)
    {
        services.AddScoped<ISearchProvider, DatabaseSearchProvider>();
        services.AddScoped<ISearchIndexer, DatabaseSearchIndexer>();
        return services;
    }
}

public sealed class DatabaseSearchProvider(CommerceDbContext dbContext) : ISearchProvider
{
    public string SystemName => DefaultSearchProviderNames.Database;

    public async Task<SearchQueryResult> SearchAsync(SearchQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildBaseQuery(request.StoreId, request.LanguageId);

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var term = request.Term.Trim();
            query = query.Where(x =>
                x.SearchText.Contains(term) ||
                x.Name.Contains(term) ||
                x.Sku.Contains(term));
        }

        query = ApplyFilters(query, request);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        query = ApplySort(query, request);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var facets = await BuildFacetsAsync(request, cancellationToken).ConfigureAwait(false);
        return new SearchQueryResult(
            items.Select(MapItem).ToList(),
            total,
            page,
            pageSize,
            facets);
    }

    public async Task<SearchSuggestionResult> SuggestAsync(SearchSuggestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Term.Length < 2)
        {
            return new SearchSuggestionResult([]);
        }

        var prefix = request.Term.Trim();
        var entries = await BuildBaseQuery(request.StoreId, request.LanguageId)
            .Where(x => x.Name.StartsWith(prefix) || x.Sku.StartsWith(prefix))
            .OrderBy(x => x.Name)
            .Take(Math.Clamp(request.MaxResults, 1, 20))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SearchSuggestionResult(
            entries.Select(x => new SearchSuggestion(x.Name, x.ProductId, x.Slug)).ToList());
    }

    private IQueryable<SearchIndexEntry> BuildBaseQuery(int storeId, int languageId) =>
        dbContext.Set<SearchIndexEntry>().AsNoTracking()
            .Where(x => x.StoreId == storeId && x.LanguageId == languageId && !x.IsDeleted && x.Published && x.IsVisible);

    private static IQueryable<SearchIndexEntry> ApplyFilters(IQueryable<SearchIndexEntry> query, SearchQueryRequest request)
    {
        if (request.CategoryId.HasValue)
        {
            var token = request.CategoryId.Value.ToString();
            query = query.Where(x => x.CategoryIdsJson.Contains(token));
        }

        if (!string.IsNullOrWhiteSpace(request.Manufacturer))
        {
            query = query.Where(x => x.Manufacturer == request.Manufacturer);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= request.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ProductType))
        {
            query = query.Where(x => x.ProductType == request.ProductType);
        }

        if (request.IsAvailable.HasValue)
        {
            query = query.Where(x => x.IsAvailable == request.IsAvailable.Value);
        }

        if (request.Attributes is { Count: > 0 })
        {
            foreach (var filter in request.Attributes)
            {
                var marker = $"\"{filter.Code}\":\"{filter.Value}\"";
                query = query.Where(x => x.AttributesJson.Contains(marker));
            }
        }

        return query;
    }

    private static IQueryable<SearchIndexEntry> ApplySort(IQueryable<SearchIndexEntry> query, SearchQueryRequest request) =>
        (request.SortField, request.SortDirection) switch
        {
            (SearchSortField.Price, SearchSortDirection.Asc) => query.OrderBy(x => x.Price ?? decimal.MaxValue),
            (SearchSortField.Price, SearchSortDirection.Desc) => query.OrderByDescending(x => x.Price ?? decimal.MinValue),
            (SearchSortField.Newest, _) => query.OrderByDescending(x => x.ProductCreatedAtUtc),
            (SearchSortField.Popularity, SearchSortDirection.Asc) => query.OrderBy(x => x.PopularityScore),
            (SearchSortField.Popularity, _) => query.OrderByDescending(x => x.PopularityScore),
            (SearchSortField.Rating, SearchSortDirection.Asc) => query.OrderBy(x => x.Rating ?? double.MinValue),
            (SearchSortField.Rating, _) => query.OrderByDescending(x => x.Rating ?? double.MinValue),
            _ => query.OrderByDescending(x => x.PopularityScore).ThenBy(x => x.Name)
        };

    private async Task<IReadOnlyList<SearchFacet>> BuildFacetsAsync(SearchQueryRequest request, CancellationToken cancellationToken)
    {
        var baseQuery = BuildBaseQuery(request.StoreId, request.LanguageId);
        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var term = request.Term.Trim();
            baseQuery = baseQuery.Where(x => x.SearchText.Contains(term));
        }

        var productTypes = await baseQuery
            .GroupBy(x => x.ProductType)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            new SearchFacet("productType", productTypes.Select(x => new SearchFacetValue(x.Key, x.Count)).ToList())
        ];
    }

    private static SearchResultItem MapItem(SearchIndexEntry entry)
    {
        var categoryIds = JsonSerializer.Deserialize<List<int>>(entry.CategoryIdsJson) ?? [];
        return new SearchResultItem(
            entry.ProductId,
            entry.Name,
            entry.Sku,
            entry.Slug,
            entry.ShortDescription,
            entry.ProductType,
            entry.Price,
            Score: 1,
            categoryIds);
    }
}

public sealed class DatabaseSearchIndexer(ISearchRepository repository) : ISearchIndexer
{
    public string SystemName => DefaultSearchProviderNames.Database;

    public Task IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default) =>
        repository.UpsertEntryAsync(SearchDocumentBuilder.ToEntry(document), cancellationToken);

    public Task DeleteDocumentAsync(int productId, int storeId, int languageId, CancellationToken cancellationToken = default) =>
        repository.DeleteEntryAsync(productId, storeId, languageId, cancellationToken);

    public async Task RebuildAsync(IReadOnlyList<SearchDocument> documents, CancellationToken cancellationToken = default)
    {
        await repository.DeleteAllEntriesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var document in documents)
        {
            await IndexDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }
}
