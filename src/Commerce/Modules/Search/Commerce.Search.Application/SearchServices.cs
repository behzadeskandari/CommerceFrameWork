using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Search;
using Commerce.Search.Application.Abstractions;
using Commerce.Search.Application.Indexing;
using Commerce.Search.Domain.Entities;
using Commerce.Search.Domain.Enums;
using Commerce.Search.Contracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Search.Application;

public sealed class SearchIndexCoordinator(
    ISearchRepository repository,
    SearchDocumentBuilder documentBuilder,
    ISearchProviderResolver providerResolver,
    ILogger<SearchIndexCoordinator> logger) : ISearchIndexCoordinator, ICatalogChangeNotifier
{
    public Task QueueProductUpsertAsync(int productId, CancellationToken cancellationToken = default)
    {
        return repository.AddJobAsync(SearchIndexJob.Create(SearchIndexJobType.ProductUpsert, productId), cancellationToken);
    }

    public Task QueueProductDeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        return repository.AddJobAsync(SearchIndexJob.Create(SearchIndexJobType.ProductDelete, productId), cancellationToken);
    }

    public Task QueueFullRebuildAsync(CancellationToken cancellationToken = default)
    {
        return repository.AddJobAsync(SearchIndexJob.Create(SearchIndexJobType.FullRebuild), cancellationToken);
    }

    public async Task ProcessPendingJobsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var jobs = await repository.ListPendingJobsAsync(batchSize, cancellationToken).ConfigureAwait(false);
        var indexer = providerResolver.ResolveIndexer();

        foreach (var job in jobs)
        {
            job.MarkProcessing();
            await repository.SaveJobAsync(job, cancellationToken).ConfigureAwait(false);

            try
            {
                switch (job.JobType)
                {
                    case SearchIndexJobType.FullRebuild:
                        await ProcessFullRebuildAsync(indexer, cancellationToken).ConfigureAwait(false);
                        break;
                    case SearchIndexJobType.ProductUpsert when job.ProductId.HasValue:
                        await ProcessProductUpsertAsync(job.ProductId.Value, indexer, cancellationToken).ConfigureAwait(false);
                        break;
                    case SearchIndexJobType.ProductDelete when job.ProductId.HasValue:
                        await ProcessProductDeleteAsync(job.ProductId.Value, cancellationToken).ConfigureAwait(false);
                        break;
                }

                job.MarkCompleted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Search index job {JobId} failed.", job.Id);
                job.MarkFailed(ex.Message);
            }

            await repository.SaveJobAsync(job, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task NotifyProductCreatedAsync(int productId, CancellationToken cancellationToken = default) =>
        QueueProductUpsertAsync(productId, cancellationToken);

    public Task NotifyProductUpdatedAsync(int productId, CancellationToken cancellationToken = default) =>
        QueueProductUpsertAsync(productId, cancellationToken);

    public Task NotifyProductDeletedAsync(int productId, CancellationToken cancellationToken = default) =>
        QueueProductDeleteAsync(productId, cancellationToken);

    private async Task ProcessFullRebuildAsync(ISearchIndexer indexer, CancellationToken cancellationToken)
    {
        var documents = await documentBuilder.BuildAllAsync(cancellationToken).ConfigureAwait(false);
        await indexer.RebuildAsync(documents, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessProductUpsertAsync(int productId, ISearchIndexer indexer, CancellationToken cancellationToken)
    {
        var documents = await documentBuilder.BuildForProductAsync(productId, cancellationToken).ConfigureAwait(false);
        foreach (var document in documents)
        {
            await indexer.IndexDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessProductDeleteAsync(int productId, CancellationToken cancellationToken)
    {
        await repository.DeleteEntriesForProductAsync(productId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class SearchQueryService(ISearchProviderResolver providerResolver) : ISearchQueryService
{
    public Task<SearchQueryResult> SearchAsync(SearchQueryRequest request, CancellationToken cancellationToken = default) =>
        providerResolver.ResolveProvider().SearchAsync(request, cancellationToken);

    public Task<SearchSuggestionResult> SuggestAsync(SearchSuggestionRequest request, CancellationToken cancellationToken = default) =>
        providerResolver.ResolveProvider().SuggestAsync(request, cancellationToken);
}
