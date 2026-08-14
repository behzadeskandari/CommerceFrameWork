using Commerce.Framework.Core.Results;
using Commerce.Framework.Scheduling;

namespace Commerce.Scheduling.Contracts.Admin;

public sealed record BackgroundJobSummaryDto(
    int Id,
    string JobType,
    BackgroundJobKind Kind,
    BackgroundJobStatus Status,
    int Priority,
    DateTime ExecuteAtUtc,
    int AttemptCount,
    int MaxAttempts,
    string? LastError,
    DateTime? NextRetryAtUtc,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? RecurringScheduleKey);

public sealed record BackgroundJobDetailDto(
    int Id,
    string JobType,
    BackgroundJobKind Kind,
    BackgroundJobStatus Status,
    string? PayloadJson,
    int Priority,
    DateTime ExecuteAtUtc,
    int AttemptCount,
    int MaxAttempts,
    string? LastError,
    DateTime? NextRetryAtUtc,
    string? IdempotencyKey,
    string? RecurringScheduleKey,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    IReadOnlyList<BackgroundJobExecutionDto> Executions);

public sealed record BackgroundJobExecutionDto(
    int Id,
    int AttemptNumber,
    BackgroundJobExecutionStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorMessage);

public sealed record RecurringJobScheduleSummaryDto(
    int Id,
    string ScheduleKey,
    string JobType,
    int IntervalSeconds,
    int MaxAttempts,
    bool IsEnabled,
    DateTime NextRunAtUtc,
    DateTime? LastRunAtUtc);

public interface IBackgroundJobAdminService
{
    Task<Result<IReadOnlyList<BackgroundJobSummaryDto>>> ListJobsAsync(
        BackgroundJobStatus? status,
        string? jobType,
        int take = 100,
        CancellationToken cancellationToken = default);

    Task<Result<BackgroundJobDetailDto>> GetJobAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> CancelJobAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> RetryJobAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RecurringJobScheduleSummaryDto>>> ListRecurringAsync(
        CancellationToken cancellationToken = default);

    Task<Result> EnableRecurringAsync(string scheduleKey, CancellationToken cancellationToken = default);

    Task<Result> DisableRecurringAsync(string scheduleKey, CancellationToken cancellationToken = default);
}
