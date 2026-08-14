using Commerce.Framework.Search;
using Commerce.Search.Application.Abstractions;
using Commerce.Search.Contracts;
using Commerce.Search.Contracts.Admin;
using Commerce.Search.Contracts.Storefront;
using Commerce.Search.Domain.Enums;

namespace Commerce.Search.Application.Storefront;

public sealed class SearchStorefrontService(ISearchQueryService queryService) : ISearchStorefrontService
{
    private const int MinSuggestionLength = 2;

    public async Task<ProductSearchResponseDto> SearchProductsAsync(
        ProductSearchRequestDto request,
        int storeId,
        int languageId,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchQueryRequest(
            request.Term,
            storeId,
            languageId,
            request.Page,
            request.PageSize,
            ParseSortField(request.SortField),
            ParseSortDirection(request.SortDirection),
            request.CategoryId,
            request.Manufacturer,
            request.MinPrice,
            request.MaxPrice,
            request.ProductType,
            request.IsAvailable,
            request.Attributes?.Select(x => new SearchAttributeFilter(x.Code, x.Value)).ToList());

        var result = await queryService.SearchAsync(query, cancellationToken).ConfigureAwait(false);
        return new ProductSearchResponseDto(
            result.Items.Select(item => new ProductSearchResultItemDto(
                item.ProductId,
                item.Name,
                item.Sku,
                item.Slug,
                item.ShortDescription,
                item.ProductType,
                item.Price,
                item.CategoryIds)).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.Facets.Select(f => new SearchFacetDto(f.Name, f.Values.Select(v => new SearchFacetValueDto(v.Value, v.Count)).ToList())).ToList());
    }

    public async Task<SearchSuggestionResponseDto> SuggestAsync(
        string term,
        int storeId,
        int languageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < MinSuggestionLength)
        {
            return new SearchSuggestionResponseDto([]);
        }

        var result = await queryService.SuggestAsync(
            new SearchSuggestionRequest(term.Trim(), storeId, languageId),
            cancellationToken).ConfigureAwait(false);

        return new SearchSuggestionResponseDto(
            result.Suggestions.Select(s => new SearchSuggestionItemDto(s.Text, s.ProductId, s.Slug)).ToList());
    }

    private static SearchSortField ParseSortField(string value) =>
        Enum.TryParse<SearchSortField>(value, true, out var parsed) ? parsed : SearchSortField.Relevance;

    private static SearchSortDirection ParseSortDirection(string value) =>
        Enum.TryParse<SearchSortDirection>(value, true, out var parsed) ? parsed : SearchSortDirection.Desc;
}

public sealed class SearchAdminService(ISearchRepository repository, ISearchIndexCoordinator coordinator) : ISearchAdminService
{
    public async Task<SearchIndexStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return new SearchIndexStatusDto(
            await repository.CountEntriesAsync(cancellationToken).ConfigureAwait(false),
            await repository.CountJobsByStatusAsync(SearchIndexJobStatus.Pending, cancellationToken).ConfigureAwait(false),
            await repository.CountJobsByStatusAsync(SearchIndexJobStatus.Failed, cancellationToken).ConfigureAwait(false),
            await repository.GetLastIndexedAtUtcAsync(cancellationToken).ConfigureAwait(false));
    }

    public Task QueueFullRebuildAsync(CancellationToken cancellationToken = default) =>
        coordinator.QueueFullRebuildAsync(cancellationToken);

    public Task ProcessPendingJobsAsync(int batchSize = 20, CancellationToken cancellationToken = default) =>
        coordinator.ProcessPendingJobsAsync(batchSize, cancellationToken);
}
