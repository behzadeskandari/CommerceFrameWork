using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Domain.Entities;

namespace Commerce.Scheduling.Application.Abstractions;

public interface ISchedulingRepository
{
    Task<BackgroundJob?> GetJobByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BackgroundJob?> GetJobByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackgroundJob>> ListDueJobsAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackgroundJob>> ListJobsAsync(
        BackgroundJobStatus? status,
        string? jobType,
        int take,
        CancellationToken cancellationToken = default);

    Task AddJobAsync(BackgroundJob job, CancellationToken cancellationToken = default);

    Task SaveJobAsync(BackgroundJob job, CancellationToken cancellationToken = default);

    Task<bool> TryClaimJobAsync(int jobId, string ownerId, DateTime lockUntilUtc, DateTime utcNow, CancellationToken cancellationToken = default);

    Task AddExecutionAsync(BackgroundJobExecution execution, CancellationToken cancellationToken = default);

    Task SaveExecutionAsync(BackgroundJobExecution execution, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackgroundJobExecution>> ListExecutionsForJobAsync(int jobId, CancellationToken cancellationToken = default);

    Task<RecurringJobSchedule?> GetRecurringByKeyAsync(string scheduleKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringJobSchedule>> ListDueRecurringAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringJobSchedule>> ListRecurringAsync(CancellationToken cancellationToken = default);

    Task AddRecurringAsync(RecurringJobSchedule schedule, CancellationToken cancellationToken = default);

    Task SaveRecurringAsync(RecurringJobSchedule schedule, CancellationToken cancellationToken = default);

    Task<bool> TryAcquireDistributedLockAsync(string lockKey, string ownerId, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    Task ReleaseDistributedLockAsync(string lockKey, string ownerId, CancellationToken cancellationToken = default);

    Task CleanupExpiredLocksAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
