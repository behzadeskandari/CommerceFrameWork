using Commerce.Framework.Search;

namespace Commerce.Search.Contracts;

public interface ISearchQueryService
{
    Task<SearchQueryResult> SearchAsync(SearchQueryRequest request, CancellationToken cancellationToken = default);

    Task<SearchSuggestionResult> SuggestAsync(SearchSuggestionRequest request, CancellationToken cancellationToken = default);
}

public interface ISearchIndexCoordinator
{
    Task QueueProductUpsertAsync(int productId, CancellationToken cancellationToken = default);

    Task QueueProductDeleteAsync(int productId, CancellationToken cancellationToken = default);

    Task QueueFullRebuildAsync(CancellationToken cancellationToken = default);

    Task ProcessPendingJobsAsync(int batchSize, CancellationToken cancellationToken = default);
}
