using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Application.Processing;
using Commerce.Scheduling.Domain.Entities;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Scheduling.Application.Scheduling;

public sealed class BackgroundJobScheduler(
    ISchedulingRepository repository,
    ICorrelationContext correlationContext) : IBackgroundJobScheduler
{
    public async Task<Result<int>> EnqueueAsync(EnqueueBackgroundJobRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await repository
                    .GetJobByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken)
                    .ConfigureAwait(false);

                if (existing is not null && existing.Status is not BackgroundJobStatus.Completed and not BackgroundJobStatus.Cancelled and not BackgroundJobStatus.DeadLetter)
                {
                    return Result.Success(existing.Id);
                }
            }

            var job = BackgroundJob.CreateImmediate(
                request.JobType,
                JobObservabilityPayload.EnrichPayload(request.PayloadJson, correlationContext.CorrelationId),
                request.Priority,
                request.MaxAttempts,
                request.IdempotencyKey);

            await repository.AddJobAsync(job, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(job.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<int>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<int>> ScheduleAsync(ScheduleBackgroundJobRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await repository
                    .GetJobByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken)
                    .ConfigureAwait(false);

                if (existing is not null && existing.Status is not BackgroundJobStatus.Completed and not BackgroundJobStatus.Cancelled and not BackgroundJobStatus.DeadLetter)
                {
                    return Result.Success(existing.Id);
                }
            }

            var job = BackgroundJob.CreateScheduled(
                request.JobType,
                request.ExecuteAtUtc,
                JobObservabilityPayload.EnrichPayload(request.PayloadJson, correlationContext.CorrelationId),
                request.Priority,
                request.MaxAttempts,
                request.IdempotencyKey);

            await repository.AddJobAsync(job, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(job.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<int>(Error.Validation(ex.Message));
        }
    }

    public Task<Result<int>> EnqueueDelayedAsync(
        string jobType,
        TimeSpan delay,
        string? payloadJson = null,
        int priority = 0,
        int maxAttempts = 3,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var executeAt = DateTime.UtcNow.Add(delay);
        return ScheduleAsync(
            new ScheduleBackgroundJobRequest(jobType, executeAt, payloadJson, priority, maxAttempts, idempotencyKey),
            cancellationToken);
    }

    public async Task<Result> RegisterRecurringAsync(RegisterRecurringJobRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await repository
                .GetRecurringByKeyAsync(request.ScheduleKey, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                return Result.Success();
            }

            var schedule = RecurringJobSchedule.Create(
                request.ScheduleKey,
                request.JobType,
                request.IntervalSeconds,
                request.PayloadJson,
                request.MaxAttempts,
                request.IsEnabled);

            await repository.AddRecurringAsync(schedule, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> CancelAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetJobByIdAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return Result.Failure(Error.NotFound("Job not found."));
        }

        if (job.Status is BackgroundJobStatus.Completed or BackgroundJobStatus.Cancelled or BackgroundJobStatus.DeadLetter)
        {
            return Result.Failure(Error.Validation("Job cannot be cancelled in its current state."));
        }

        job.MarkCancelled("Cancelled by request.");
        await repository.SaveJobAsync(job, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
