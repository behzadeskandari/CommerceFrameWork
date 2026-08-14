namespace Commerce.Framework.Search;

public enum SearchSortField
{
    Relevance = 0,
    Price = 1,
    Newest = 2,
    Popularity = 3,
    Rating = 4
}

public enum SearchSortDirection
{
    Asc = 0,
    Desc = 1
}

public sealed record SearchAttributeFilter(string Code, string Value);

public sealed record SearchQueryRequest(
    string? Term,
    int StoreId,
    int LanguageId,
    int Page = 1,
    int PageSize = 20,
    SearchSortField SortField = SearchSortField.Relevance,
    SearchSortDirection SortDirection = SearchSortDirection.Desc,
    int? CategoryId = null,
    string? Manufacturer = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? ProductType = null,
    bool? IsAvailable = null,
    IReadOnlyList<SearchAttributeFilter>? Attributes = null);

public sealed record SearchSuggestionRequest(
    string Term,
    int StoreId,
    int LanguageId,
    int MaxResults = 8);

public sealed record SearchDocument(
    int ProductId,
    int StoreId,
    int LanguageId,
    string Name,
    string Sku,
    string? Slug,
    string? Description,
    string? ShortDescription,
    IReadOnlyList<int> CategoryIds,
    IReadOnlyList<string> CategoryNames,
    string? Manufacturer,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Attributes,
    string ProductType,
    decimal? Price,
    bool Published,
    bool IsVisible,
    bool IsAvailable,
    double PopularityScore,
    double? Rating,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool IsDeleted,
    string SearchText);

public sealed record SearchResultItem(
    int ProductId,
    string Name,
    string Sku,
    string? Slug,
    string? ShortDescription,
    string ProductType,
    decimal? Price,
    double Score,
    IReadOnlyList<int> CategoryIds);

public sealed record SearchFacetValue(string Value, int Count);

public sealed record SearchFacet(string Name, IReadOnlyList<SearchFacetValue> Values);

public sealed record SearchQueryResult(
    IReadOnlyList<SearchResultItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<SearchFacet> Facets);

public sealed record SearchSuggestion(string Text, int ProductId, string? Slug);

public sealed record SearchSuggestionResult(IReadOnlyList<SearchSuggestion> Suggestions);

public interface ISearchProvider
{
    string SystemName { get; }

    Task<SearchQueryResult> SearchAsync(SearchQueryRequest request, CancellationToken cancellationToken = default);

    Task<SearchSuggestionResult> SuggestAsync(SearchSuggestionRequest request, CancellationToken cancellationToken = default);
}

public interface ISearchIndexer
{
    string SystemName { get; }

    Task IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(int productId, int storeId, int languageId, CancellationToken cancellationToken = default);

    Task RebuildAsync(IReadOnlyList<SearchDocument> documents, CancellationToken cancellationToken = default);
}

public interface ISearchProviderResolver
{
    ISearchProvider ResolveProvider();

    ISearchIndexer ResolveIndexer();
}

public static class DefaultSearchProviderNames
{
    public const string Database = "Search.Database";
}
