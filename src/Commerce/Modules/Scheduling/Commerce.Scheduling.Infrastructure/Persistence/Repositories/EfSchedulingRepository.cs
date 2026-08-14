using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Scheduling.Infrastructure.Persistence.Repositories;

public sealed class EfSchedulingRepository(CommerceDbContext dbContext) : ISchedulingRepository
{
    public Task<BackgroundJob?> GetJobByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<BackgroundJob>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<BackgroundJob?> GetJobByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        dbContext.Set<BackgroundJob>().FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<IReadOnlyList<BackgroundJob>> ListDueJobsAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<BackgroundJob>()
            .Where(x =>
                (x.Status == BackgroundJobStatus.Pending ||
                 x.Status == BackgroundJobStatus.Scheduled ||
                 x.Status == BackgroundJobStatus.Failed) &&
                x.ExecuteAtUtc <= utcNow &&
                (x.NextRetryAtUtc == null || x.NextRetryAtUtc <= utcNow) &&
                (x.LockedUntilUtc == null || x.LockedUntilUtc <= utcNow))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.ExecuteAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<BackgroundJob>)t.Result, cancellationToken);
    }

    public Task<IReadOnlyList<BackgroundJob>> ListJobsAsync(
        BackgroundJobStatus? status,
        string? jobType,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<BackgroundJob>().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(jobType))
        {
            query = query.Where(x => x.JobType == jobType);
        }

        return query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<BackgroundJob>)t.Result, cancellationToken);
    }

    public Task AddJobAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        dbContext.Set<BackgroundJob>().Add(job);
        return Task.CompletedTask;
    }

    public Task SaveJobAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        dbContext.Set<BackgroundJob>().Update(job);
        return Task.CompletedTask;
    }

    public async Task<bool> TryClaimJobAsync(
        int jobId,
        string ownerId,
        DateTime lockUntilUtc,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE BackgroundJob
             SET Status = {(int)BackgroundJobStatus.Running},
                 LockOwnerId = {ownerId},
                 LockedUntilUtc = {lockUntilUtc},
                 StartedAtUtc = {utcNow},
                 UpdatedAtUtc = {utcNow}
             WHERE Id = {jobId}
               AND Status IN ({(int)BackgroundJobStatus.Pending}, {(int)BackgroundJobStatus.Scheduled}, {(int)BackgroundJobStatus.Failed})
               AND ExecuteAtUtc <= {utcNow}
               AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= {utcNow})
               AND (LockedUntilUtc IS NULL OR LockedUntilUtc <= {utcNow})
             """,
            cancellationToken).ConfigureAwait(false);

        return rows > 0;
    }

    public Task AddExecutionAsync(BackgroundJobExecution execution, CancellationToken cancellationToken = default)
    {
        dbContext.Set<BackgroundJobExecution>().Add(execution);
        return Task.CompletedTask;
    }

    public Task SaveExecutionAsync(BackgroundJobExecution execution, CancellationToken cancellationToken = default)
    {
        dbContext.Set<BackgroundJobExecution>().Update(execution);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BackgroundJobExecution>> ListExecutionsForJobAsync(int jobId, CancellationToken cancellationToken = default) =>
        dbContext.Set<BackgroundJobExecution>()
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.AttemptNumber)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<BackgroundJobExecution>)t.Result, cancellationToken);

    public Task<RecurringJobSchedule?> GetRecurringByKeyAsync(string scheduleKey, CancellationToken cancellationToken = default) =>
        dbContext.Set<RecurringJobSchedule>()
            .FirstOrDefaultAsync(x => x.ScheduleKey == scheduleKey.Trim().ToLowerInvariant(), cancellationToken);

    public Task<IReadOnlyList<RecurringJobSchedule>> ListDueRecurringAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        dbContext.Set<RecurringJobSchedule>()
            .Where(x => x.IsEnabled && x.NextRunAtUtc <= utcNow)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<RecurringJobSchedule>)t.Result, cancellationToken);

    public Task<IReadOnlyList<RecurringJobSchedule>> ListRecurringAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<RecurringJobSchedule>()
            .OrderBy(x => x.ScheduleKey)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<RecurringJobSchedule>)t.Result, cancellationToken);

    public Task AddRecurringAsync(RecurringJobSchedule schedule, CancellationToken cancellationToken = default)
    {
        dbContext.Set<RecurringJobSchedule>().Add(schedule);
        return Task.CompletedTask;
    }

    public Task SaveRecurringAsync(RecurringJobSchedule schedule, CancellationToken cancellationToken = default)
    {
        dbContext.Set<RecurringJobSchedule>().Update(schedule);
        return Task.CompletedTask;
    }

    public async Task<bool> TryAcquireDistributedLockAsync(
        string lockKey,
        string ownerId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        await CleanupExpiredLocksAsync(DateTime.UtcNow, cancellationToken).ConfigureAwait(false);

        var existing = await dbContext.Set<JobDistributedLock>()
            .FirstOrDefaultAsync(x => x.LockKey == lockKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null && !existing.IsExpired(DateTime.UtcNow))
        {
            return false;
        }

        if (existing is not null)
        {
            dbContext.Set<JobDistributedLock>().Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            dbContext.Set<JobDistributedLock>().Add(JobDistributedLock.Create(lockKey, ownerId, DateTime.UtcNow, expiresAtUtc));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task ReleaseDistributedLockAsync(string lockKey, string ownerId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<JobDistributedLock>()
            .FirstOrDefaultAsync(x => x.LockKey == lockKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(ownerId) &&
            !string.Equals(existing.OwnerId, ownerId, StringComparison.Ordinal))
        {
            return;
        }

        dbContext.Set<JobDistributedLock>().Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CleanupExpiredLocksAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expired = await dbContext.Set<JobDistributedLock>()
            .Where(x => x.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expired.Count == 0)
        {
            return;
        }

        dbContext.Set<JobDistributedLock>().RemoveRange(expired);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
