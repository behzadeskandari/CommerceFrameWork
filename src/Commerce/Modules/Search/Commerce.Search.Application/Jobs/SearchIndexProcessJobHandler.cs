using Commerce.Framework.Scheduling;
using Commerce.Search.Contracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Search.Application.Jobs;

public sealed class SearchIndexProcessJobHandler(
    ISearchIndexCoordinator coordinator,
    ILogger<SearchIndexProcessJobHandler> logger) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.SearchIndexProcess;

    public async Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await coordinator.ProcessPendingJobsAsync(20, cancellationToken).ConfigureAwait(false);
            return new BackgroundJobHandleResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search index background job failed.");
            return new BackgroundJobHandleResult(false, ex.Message, RetryRequested: true, RetryDelay: TimeSpan.FromMinutes(1));
        }
    }
}
