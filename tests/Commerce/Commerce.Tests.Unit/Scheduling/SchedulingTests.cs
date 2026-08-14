using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Application.Execution;
using Commerce.Scheduling.Application.Scheduling;
using Commerce.Scheduling.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commerce.Tests.Unit.Scheduling;

public sealed class BackgroundJobSchedulerTests
{
    [Fact]
    public async Task EnqueueAsync_ReturnsExistingJob_WhenIdempotencyKeyMatches()
    {
        var repository = new FakeSchedulingRepository();
        var existing = BackgroundJob.CreateImmediate("email.send", null, 0, 3, "idem-1");
        repository.Jobs.Add(existing);
        var scheduler = new BackgroundJobScheduler(repository, new NoOpCorrelationContext());

        var result = await scheduler.EnqueueAsync(
            new EnqueueBackgroundJobRequest("email.send", IdempotencyKey: "idem-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value);
        Assert.Single(repository.Jobs);
    }

    [Fact]
    public async Task CancelAsync_MarksJobCancelled()
    {
        var repository = new FakeSchedulingRepository();
        var job = BackgroundJob.CreateImmediate("email.send", null, 0, 3, null);
        repository.Jobs.Add(job);
        var scheduler = new BackgroundJobScheduler(repository, new NoOpCorrelationContext());

        var result = await scheduler.CancelAsync(job.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundJobStatus.Cancelled, job.Status);
    }
}

public sealed class BackgroundJobExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_MarksCompleted_WhenHandlerSucceeds()
    {
        var repository = new FakeSchedulingRepository();
        var executor = new BackgroundJobExecutor(
            [new SuccessHandler()],
            repository,
            NullLogger<BackgroundJobExecutor>.Instance);

        var job = BackgroundJob.CreateImmediate("test.success", null, 0, 3, null);
        repository.Jobs.Add(job);

        await executor.ExecuteAsync(job, "worker-1", CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Completed, job.Status);
        Assert.Single(repository.Executions);
    }

    [Fact]
    public async Task ExecuteAsync_MovesToDeadLetter_AfterMaxAttempts()
    {
        var repository = new FakeSchedulingRepository();
        var executor = new BackgroundJobExecutor(
            [new FailingHandler()],
            repository,
            NullLogger<BackgroundJobExecutor>.Instance);

        var job = BackgroundJob.CreateImmediate("test.fail", null, 0, 1, null);
        repository.Jobs.Add(job);

        await executor.ExecuteAsync(job, "worker-1", CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.DeadLetter, job.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SchedulesRetry_WhenBelowMaxAttempts()
    {
        var repository = new FakeSchedulingRepository();
        var executor = new BackgroundJobExecutor(
            [new FailingHandler()],
            repository,
            NullLogger<BackgroundJobExecutor>.Instance);

        var job = BackgroundJob.CreateImmediate("test.fail", null, 0, 3, null);
        repository.Jobs.Add(job);

        await executor.ExecuteAsync(job, "worker-1", CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Failed, job.Status);
        Assert.NotNull(job.NextRetryAtUtc);
    }
}

public sealed class BackgroundJobDomainTests
{
    [Fact]
    public void TryClaim_PreventsDuplicateExecution_WhenAlreadyRunning()
    {
        var repository = new FakeSchedulingRepository();
        var job = BackgroundJob.CreateImmediate("inventory.tasks", null, 0, 3, null);
        repository.Jobs.Add(job);

        var first = repository.TryClaimJobAsync(job.Id, "worker-a", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow, CancellationToken.None).Result;
        var second = repository.TryClaimJobAsync(job.Id, "worker-b", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow, CancellationToken.None).Result;

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void PrepareForManualRetry_ResetsFailedJob()
    {
        var job = BackgroundJob.CreateImmediate("reports.generate", null, 0, 3, null);
        job.RecordAttempt();
        job.MarkFailed("error", DateTime.UtcNow.AddMinutes(5));

        job.PrepareForManualRetry();

        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
        Assert.Equal(0, job.AttemptCount);
    }

    [Fact]
    public void IdempotencyKey_PreventsDuplicateEnqueue()
    {
        var repository = new FakeSchedulingRepository();
        var scheduler = new BackgroundJobScheduler(repository, new NoOpCorrelationContext());

        var first = scheduler.EnqueueAsync(new EnqueueBackgroundJobRequest("cleanup", IdempotencyKey: "once")).Result;
        var second = scheduler.EnqueueAsync(new EnqueueBackgroundJobRequest("cleanup", IdempotencyKey: "once")).Result;

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
        Assert.Single(repository.Jobs);
    }
}

public sealed class SchedulingAuthorizationTests
{
    [Fact]
    public void SchedulingPermissions_AreDefined()
    {
        Assert.Equal("Scheduling.View", Commerce.Scheduling.Infrastructure.Security.SchedulingPermissions.View);
        Assert.Equal("Scheduling.Manage", Commerce.Scheduling.Infrastructure.Security.SchedulingPermissions.Manage);
    }
}

internal sealed class FakeSchedulingRepository : ISchedulingRepository
{
    public List<BackgroundJob> Jobs { get; } = [];
    public List<BackgroundJobExecution> Executions { get; } = [];
    public List<RecurringJobSchedule> Recurring { get; } = [];
    public HashSet<string> Locks { get; } = new(StringComparer.Ordinal);

    public Task<BackgroundJob?> GetJobByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Jobs.FirstOrDefault(x => x.Id == id));

    public Task<BackgroundJob?> GetJobByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Jobs.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey));

    public Task<IReadOnlyList<BackgroundJob>> ListDueJobsAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BackgroundJob>>(Jobs.Where(x => x.IsDue(utcNow)).Take(take).ToList());

    public Task<IReadOnlyList<BackgroundJob>> ListJobsAsync(BackgroundJobStatus? status, string? jobType, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BackgroundJob>>(Jobs.Take(take).ToList());

    public Task AddJobAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        if (job.Id == 0)
        {
            job.GetType().GetProperty(nameof(BackgroundJob.Id))!.SetValue(job, Jobs.Count + 1);
        }
        Jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task SaveJobAsync(BackgroundJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> TryClaimJobAsync(int jobId, string ownerId, DateTime lockUntilUtc, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var job = Jobs.FirstOrDefault(x => x.Id == jobId);
        if (job is null || !job.CanBeClaimed(utcNow))
        {
            return Task.FromResult(false);
        }

        job.Claim(ownerId, lockUntilUtc);
        return Task.FromResult(true);
    }

    public Task AddExecutionAsync(BackgroundJobExecution execution, CancellationToken cancellationToken = default)
    {
        Executions.Add(execution);
        return Task.CompletedTask;
    }

    public Task SaveExecutionAsync(BackgroundJobExecution execution, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<BackgroundJobExecution>> ListExecutionsForJobAsync(int jobId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BackgroundJobExecution>>(Executions.Where(x => x.JobId == jobId).ToList());

    public Task<RecurringJobSchedule?> GetRecurringByKeyAsync(string scheduleKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Recurring.FirstOrDefault(x => x.ScheduleKey == scheduleKey));

    public Task<IReadOnlyList<RecurringJobSchedule>> ListDueRecurringAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecurringJobSchedule>>(Recurring.Where(x => x.IsDue(utcNow)).ToList());

    public Task<IReadOnlyList<RecurringJobSchedule>> ListRecurringAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecurringJobSchedule>>(Recurring.ToList());

    public Task AddRecurringAsync(RecurringJobSchedule schedule, CancellationToken cancellationToken = default)
    {
        Recurring.Add(schedule);
        return Task.CompletedTask;
    }

    public Task SaveRecurringAsync(RecurringJobSchedule schedule, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> TryAcquireDistributedLockAsync(string lockKey, string ownerId, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        if (!Locks.Add(lockKey))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task ReleaseDistributedLockAsync(string lockKey, string ownerId, CancellationToken cancellationToken = default)
    {
        Locks.Remove(lockKey);
        return Task.CompletedTask;
    }

    public Task CleanupExpiredLocksAsync(DateTime utcNow, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class SuccessHandler : IBackgroundJobHandler
{
    public string JobType => "test.success";
    public Task<BackgroundJobHandleResult> ExecuteAsync(BackgroundJobExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BackgroundJobHandleResult(true));
}

internal sealed class FailingHandler : IBackgroundJobHandler
{
    public string JobType => "test.fail";
    public Task<BackgroundJobHandleResult> ExecuteAsync(BackgroundJobExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BackgroundJobHandleResult(false, "boom"));
}

internal sealed class NoOpCorrelationContext : ICorrelationContext
{
    public string? CorrelationId => null;
    public string? RequestId => null;
    public string? TraceId => null;
}
