using Commerce.Framework.Core.Results;

namespace Commerce.Framework.Scheduling;

public sealed record EnqueueBackgroundJobRequest(
    string JobType,
    string? PayloadJson = null,
    int Priority = 0,
    int MaxAttempts = 3,
    string? IdempotencyKey = null);

public sealed record ScheduleBackgroundJobRequest(
    string JobType,
    DateTime ExecuteAtUtc,
    string? PayloadJson = null,
    int Priority = 0,
    int MaxAttempts = 3,
    string? IdempotencyKey = null);

public sealed record RegisterRecurringJobRequest(
    string ScheduleKey,
    string JobType,
    int IntervalSeconds,
    string? PayloadJson = null,
    int MaxAttempts = 3,
    bool IsEnabled = true);

public sealed record BackgroundJobExecutionContext(
    int JobId,
    string JobType,
    string? PayloadJson,
    int AttemptNumber,
    string? IdempotencyKey,
    CancellationToken CancellationToken);

public sealed record BackgroundJobHandleResult(
    bool Success,
    string? ErrorMessage = null,
    bool RetryRequested = false,
    TimeSpan? RetryDelay = null);

public interface IBackgroundJobScheduler
{
    Task<Result<int>> EnqueueAsync(EnqueueBackgroundJobRequest request, CancellationToken cancellationToken = default);

    Task<Result<int>> ScheduleAsync(ScheduleBackgroundJobRequest request, CancellationToken cancellationToken = default);

    Task<Result<int>> EnqueueDelayedAsync(
        string jobType,
        TimeSpan delay,
        string? payloadJson = null,
        int priority = 0,
        int maxAttempts = 3,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task<Result> RegisterRecurringAsync(RegisterRecurringJobRequest request, CancellationToken cancellationToken = default);

    Task<Result> CancelAsync(int jobId, CancellationToken cancellationToken = default);
}

public interface IBackgroundJobHandler
{
    string JobType { get; }

    Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default);
}

public interface IJobLockHandle : IAsyncDisposable
{
    string LockKey { get; }
}

public interface IJobLockProvider
{
    Task<IJobLockHandle?> TryAcquireAsync(string lockKey, TimeSpan duration, CancellationToken cancellationToken = default);
}
