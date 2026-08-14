namespace Commerce.Search.Contracts.Admin;

public sealed record SearchIndexStatusDto(
    int TotalEntries,
    int PendingJobs,
    int FailedJobs,
    DateTime? LastIndexedAtUtc);

public interface ISearchAdminService
{
    Task<SearchIndexStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task QueueFullRebuildAsync(CancellationToken cancellationToken = default);

    Task ProcessPendingJobsAsync(int batchSize = 20, CancellationToken cancellationToken = default);
}
