using Commerce.Framework.Application.Observability;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Commerce.Scheduling.Application.Execution;

public sealed class BackgroundJobExecutor(
    IEnumerable<IBackgroundJobHandler> handlers,
    ISchedulingRepository repository,
    ILogger<BackgroundJobExecutor> logger)
{
    private readonly IReadOnlyDictionary<string, IBackgroundJobHandler> _handlers =
        handlers.ToDictionary(x => x.JobType, StringComparer.OrdinalIgnoreCase);

    public async Task ExecuteAsync(BackgroundJob job, string ownerId, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(job.JobType, out var handler))
        {
            job.RecordAttempt();
            job.MarkFailed($"No handler registered for job type '{job.JobType}'.", null);
            await repository.SaveJobAsync(job, cancellationToken).ConfigureAwait(false);
            return;
        }

        job.RecordAttempt();
        var execution = BackgroundJobExecution.Start(job.Id, job.AttemptCount);
        await repository.AddExecutionAsync(execution, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var correlationId = JobObservabilityPayload.ExtractCorrelationId(job.PayloadJson);
        using (CommerceLogging.BeginOperationScope(
            logger,
            new JobCorrelationContextAdapter(correlationId),
            "background.job.execute",
            ("JobId", job.Id),
            ("JobType", job.JobType)))
        {
            try
            {
                var context = new BackgroundJobExecutionContext(
                    job.Id,
                    job.JobType,
                    job.PayloadJson,
                    job.AttemptCount,
                    job.IdempotencyKey,
                    cancellationToken);

                var result = await handler.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    execution.MarkCompleted();
                    job.MarkCompleted();
                }
                else if (result.RetryRequested && job.AttemptCount < job.MaxAttempts)
                {
                    var delay = result.RetryDelay ?? CalculateRetryDelay(job.AttemptCount);
                    var nextRetry = DateTime.UtcNow.Add(delay);
                    execution.MarkFailed(result.ErrorMessage ?? "Retry requested.");
                    job.MarkFailed(result.ErrorMessage ?? "Retry requested.", nextRetry);
                }
                else
                {
                    execution.MarkFailed(result.ErrorMessage ?? "Job failed.");
                    var nextRetry = DateTime.UtcNow.Add(CalculateRetryDelay(job.AttemptCount));
                    job.MarkFailed(result.ErrorMessage ?? "Job failed.", job.AttemptCount < job.MaxAttempts ? nextRetry : null);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                execution.MarkCancelled("Cancelled.");
                job.ReleaseClaim();
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background job {JobId} ({JobType}) failed.", job.Id, job.JobType);
                execution.MarkFailed(ex.Message);
                var nextRetry = DateTime.UtcNow.Add(CalculateRetryDelay(job.AttemptCount));
                job.MarkFailed(ex.Message, job.AttemptCount < job.MaxAttempts ? nextRetry : null);
            }

            await repository.SaveExecutionAsync(execution, cancellationToken).ConfigureAwait(false);
            await repository.SaveJobAsync(job, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class JobCorrelationContextAdapter(string? correlationId) : ICorrelationContext
    {
        public string? CorrelationId => correlationId;
        public string? RequestId => correlationId;
        public string? TraceId => System.Diagnostics.Activity.Current?.TraceId.ToString();
    }

    internal static TimeSpan CalculateRetryDelay(int attemptCount) =>
        TimeSpan.FromMinutes(Math.Pow(2, Math.Max(1, attemptCount)));
}
