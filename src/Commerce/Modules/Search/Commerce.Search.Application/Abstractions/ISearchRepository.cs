using Commerce.Search.Domain.Entities;
using Commerce.Search.Domain.Enums;

namespace Commerce.Search.Application.Abstractions;

public interface ISearchRepository
{
    Task<SearchIndexEntry?> GetEntryAsync(int productId, int storeId, int languageId, CancellationToken cancellationToken = default);

    Task UpsertEntryAsync(SearchIndexEntry entry, CancellationToken cancellationToken = default);

    Task DeleteEntryAsync(int productId, int storeId, int languageId, CancellationToken cancellationToken = default);

    Task DeleteEntriesForProductAsync(int productId, CancellationToken cancellationToken = default);

    Task DeleteAllEntriesAsync(CancellationToken cancellationToken = default);

    Task<int> CountEntriesAsync(CancellationToken cancellationToken = default);

    Task<DateTime?> GetLastIndexedAtUtcAsync(CancellationToken cancellationToken = default);

    Task AddJobAsync(SearchIndexJob job, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchIndexJob>> ListPendingJobsAsync(int batchSize, CancellationToken cancellationToken = default);

    Task SaveJobAsync(SearchIndexJob job, CancellationToken cancellationToken = default);

    Task<int> CountJobsByStatusAsync(SearchIndexJobStatus status, CancellationToken cancellationToken = default);
}
