using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Application.Execution;
using Commerce.Scheduling.Contracts.Admin;
using Commerce.Scheduling.Domain.Entities;

namespace Commerce.Scheduling.Application.Admin;

public sealed class BackgroundJobAdminService(
    ISchedulingRepository repository,
    BackgroundJobExecutor executor) : IBackgroundJobAdminService
{
    public async Task<Result<IReadOnlyList<BackgroundJobSummaryDto>>> ListJobsAsync(
        BackgroundJobStatus? status,
        string? jobType,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var jobs = await repository
            .ListJobsAsync(status, jobType, Math.Clamp(take, 1, 500), cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<BackgroundJobSummaryDto>>(jobs.Select(MapSummary).ToList());
    }

    public async Task<Result<BackgroundJobDetailDto>> GetJobAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetJobByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return Result.Failure<BackgroundJobDetailDto>(Error.NotFound("Job not found."));
        }

        var executions = await repository.ListExecutionsForJobAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(job, executions));
    }

    public async Task<Result> CancelJobAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetJobByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return Result.Failure(Error.NotFound("Job not found."));
        }

        if (job.Status is BackgroundJobStatus.Completed or BackgroundJobStatus.Cancelled or BackgroundJobStatus.DeadLetter)
        {
            return Result.Failure(Error.Validation("Job cannot be cancelled."));
        }

        job.MarkCancelled("Cancelled by administrator.");
        await repository.SaveJobAsync(job, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> RetryJobAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetJobByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return Result.Failure(Error.NotFound("Job not found."));
        }

        if (job.Status is not (BackgroundJobStatus.Failed or BackgroundJobStatus.DeadLetter))
        {
            return Result.Failure(Error.Validation("Only failed or dead-letter jobs can be retried."));
        }

        job.PrepareForManualRetry();
        await repository.SaveJobAsync(job, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var claimed = await repository
            .TryClaimJobAsync(job.Id, "admin-retry", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        if (!claimed)
        {
            return Result.Failure(Error.Conflict("Job could not be claimed for retry."));
        }

        var refreshed = await repository.GetJobByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (refreshed is null)
        {
            return Result.Failure(Error.NotFound("Job not found."));
        }

        await executor.ExecuteAsync(refreshed, "admin-retry", cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<RecurringJobScheduleSummaryDto>>> ListRecurringAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await repository.ListRecurringAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<RecurringJobScheduleSummaryDto>>(items.Select(MapRecurring).ToList());
    }

    public async Task<Result> EnableRecurringAsync(string scheduleKey, CancellationToken cancellationToken = default)
    {
        var schedule = await repository.GetRecurringByKeyAsync(scheduleKey, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound("Recurring schedule not found."));
        }

        schedule.Enable();
        await repository.SaveRecurringAsync(schedule, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DisableRecurringAsync(string scheduleKey, CancellationToken cancellationToken = default)
    {
        var schedule = await repository.GetRecurringByKeyAsync(scheduleKey, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound("Recurring schedule not found."));
        }

        schedule.Disable();
        await repository.SaveRecurringAsync(schedule, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static BackgroundJobSummaryDto MapSummary(BackgroundJob job) =>
        new(
            job.Id,
            job.JobType,
            job.Kind,
            job.Status,
            job.Priority,
            job.ExecuteAtUtc,
            job.AttemptCount,
            job.MaxAttempts,
            job.LastError,
            job.NextRetryAtUtc,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.RecurringScheduleKey);

    private static BackgroundJobDetailDto MapDetail(BackgroundJob job, IReadOnlyList<BackgroundJobExecution> executions) =>
        new(
            job.Id,
            job.JobType,
            job.Kind,
            job.Status,
            job.PayloadJson,
            job.Priority,
            job.ExecuteAtUtc,
            job.AttemptCount,
            job.MaxAttempts,
            job.LastError,
            job.NextRetryAtUtc,
            job.IdempotencyKey,
            job.RecurringScheduleKey,
            job.CreatedAtUtc,
            job.UpdatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.CancelledAtUtc,
            executions.Select(x => new BackgroundJobExecutionDto(
                x.Id,
                x.AttemptNumber,
                x.Status,
                x.StartedAtUtc,
                x.CompletedAtUtc,
                x.ErrorMessage)).ToList());

    private static RecurringJobScheduleSummaryDto MapRecurring(RecurringJobSchedule schedule) =>
        new(
            schedule.Id,
            schedule.ScheduleKey,
            schedule.JobType,
            schedule.IntervalSeconds,
            schedule.MaxAttempts,
            schedule.IsEnabled,
            schedule.NextRunAtUtc,
            schedule.LastRunAtUtc);
}
