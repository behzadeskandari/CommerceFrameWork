namespace Commerce.Search.Contracts.Storefront;

public sealed record ProductSearchRequestDto(
    string? Term,
    int Page = 1,
    int PageSize = 20,
    string SortField = "Relevance",
    string SortDirection = "Desc",
    int? CategoryId = null,
    string? Manufacturer = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? ProductType = null,
    bool? IsAvailable = null,
    IReadOnlyList<SearchAttributeFilterDto>? Attributes = null);

public sealed record SearchAttributeFilterDto(string Code, string Value);

public sealed record ProductSearchResultItemDto(
    int ProductId,
    string Name,
    string Sku,
    string? Slug,
    string? ShortDescription,
    string ProductType,
    decimal? Price,
    IReadOnlyList<int> CategoryIds);

public sealed record ProductSearchResponseDto(
    IReadOnlyList<ProductSearchResultItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<SearchFacetDto> Facets);

public sealed record SearchFacetDto(string Name, IReadOnlyList<SearchFacetValueDto> Values);

public sealed record SearchFacetValueDto(string Value, int Count);

public sealed record SearchSuggestionResponseDto(IReadOnlyList<SearchSuggestionItemDto> Suggestions);

public sealed record SearchSuggestionItemDto(string Text, int ProductId, string? Slug);

public interface ISearchStorefrontService
{
    Task<ProductSearchResponseDto> SearchProductsAsync(ProductSearchRequestDto request, int storeId, int languageId, CancellationToken cancellationToken = default);

    Task<SearchSuggestionResponseDto> SuggestAsync(string term, int storeId, int languageId, CancellationToken cancellationToken = default);
}
