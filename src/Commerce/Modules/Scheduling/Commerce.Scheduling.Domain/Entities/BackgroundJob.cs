using Commerce.Framework.Core.Entities;
using Commerce.Framework.Scheduling;

namespace Commerce.Scheduling.Domain.Entities;

public sealed class BackgroundJob : AggregateRoot
{
    public const int JobTypeMaxLength = 128;
    public const int PayloadMaxLength = 16000;
    public const int IdempotencyKeyMaxLength = 256;
    public const int LockOwnerMaxLength = 128;
    public const int ErrorMaxLength = 2000;
    public const int ScheduleKeyMaxLength = 128;

    public string JobType { get; private set; } = string.Empty;

    public BackgroundJobKind Kind { get; private set; }

    public BackgroundJobStatus Status { get; private set; }

    public string? PayloadJson { get; private set; }

    public int Priority { get; private set; }

    public DateTime ExecuteAtUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public int MaxAttempts { get; private set; }

    public string? LastError { get; private set; }

    public DateTime? NextRetryAtUtc { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public string? LockOwnerId { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public string? RecurringScheduleKey { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public static BackgroundJob CreateImmediate(
        string jobType,
        string? payloadJson,
        int priority,
        int maxAttempts,
        string? idempotencyKey)
    {
        ValidateJobType(jobType);
        var utcNow = DateTime.UtcNow;
        return new BackgroundJob
        {
            JobType = jobType.Trim(),
            Kind = BackgroundJobKind.Immediate,
            Status = BackgroundJobStatus.Pending,
            PayloadJson = NormalizePayload(payloadJson),
            Priority = priority,
            ExecuteAtUtc = utcNow,
            AttemptCount = 0,
            MaxAttempts = Math.Max(1, maxAttempts),
            IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public static BackgroundJob CreateDelayed(
        string jobType,
        DateTime executeAtUtc,
        string? payloadJson,
        int priority,
        int maxAttempts,
        string? idempotencyKey)
    {
        ValidateJobType(jobType);
        var utcNow = DateTime.UtcNow;
        return new BackgroundJob
        {
            JobType = jobType.Trim(),
            Kind = BackgroundJobKind.Delayed,
            Status = BackgroundJobStatus.Scheduled,
            PayloadJson = NormalizePayload(payloadJson),
            Priority = priority,
            ExecuteAtUtc = executeAtUtc,
            AttemptCount = 0,
            MaxAttempts = Math.Max(1, maxAttempts),
            IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public static BackgroundJob CreateScheduled(
        string jobType,
        DateTime executeAtUtc,
        string? payloadJson,
        int priority,
        int maxAttempts,
        string? idempotencyKey)
    {
        ValidateJobType(jobType);
        var utcNow = DateTime.UtcNow;
        return new BackgroundJob
        {
            JobType = jobType.Trim(),
            Kind = BackgroundJobKind.Scheduled,
            Status = BackgroundJobStatus.Scheduled,
            PayloadJson = NormalizePayload(payloadJson),
            Priority = priority,
            ExecuteAtUtc = executeAtUtc,
            AttemptCount = 0,
            MaxAttempts = Math.Max(1, maxAttempts),
            IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public static BackgroundJob CreateFromRecurring(
        string jobType,
        string scheduleKey,
        string? payloadJson,
        int priority,
        int maxAttempts,
        string? idempotencyKey)
    {
        ValidateJobType(jobType);
        var utcNow = DateTime.UtcNow;
        return new BackgroundJob
        {
            JobType = jobType.Trim(),
            Kind = BackgroundJobKind.Recurring,
            Status = BackgroundJobStatus.Pending,
            PayloadJson = NormalizePayload(payloadJson),
            Priority = priority,
            ExecuteAtUtc = utcNow,
            AttemptCount = 0,
            MaxAttempts = Math.Max(1, maxAttempts),
            IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey),
            RecurringScheduleKey = scheduleKey.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public bool IsDue(DateTime utcNow) =>
        Status is BackgroundJobStatus.Pending or BackgroundJobStatus.Scheduled or BackgroundJobStatus.Failed &&
        ExecuteAtUtc <= utcNow &&
        (NextRetryAtUtc is null || NextRetryAtUtc <= utcNow) &&
        (LockedUntilUtc is null || LockedUntilUtc <= utcNow);

    public bool CanBeClaimed(DateTime utcNow) =>
        Status is BackgroundJobStatus.Pending or BackgroundJobStatus.Scheduled or BackgroundJobStatus.Failed &&
        IsDue(utcNow);

    public void Claim(string ownerId, DateTime lockUntilUtc)
    {
        Status = BackgroundJobStatus.Running;
        LockOwnerId = ownerId;
        LockedUntilUtc = lockUntilUtc;
        StartedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void RecordAttempt()
    {
        AttemptCount++;
        Touch();
    }

    public void MarkCompleted()
    {
        Status = BackgroundJobStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        NextRetryAtUtc = null;
        LastError = null;
        LockOwnerId = null;
        LockedUntilUtc = null;
        Touch();
    }

    public void MarkFailed(string error, DateTime? nextRetryAtUtc)
    {
        LastError = Truncate(error, ErrorMaxLength);
        UpdatedAtUtc = DateTime.UtcNow;
        LockOwnerId = null;
        LockedUntilUtc = null;

        if (AttemptCount >= MaxAttempts || !nextRetryAtUtc.HasValue)
        {
            Status = BackgroundJobStatus.DeadLetter;
            NextRetryAtUtc = null;
            CompletedAtUtc = DateTime.UtcNow;
            return;
        }

        Status = BackgroundJobStatus.Failed;
        NextRetryAtUtc = nextRetryAtUtc;
        ExecuteAtUtc = nextRetryAtUtc.Value;
    }

    public void MarkCancelled(string reason)
    {
        Status = BackgroundJobStatus.Cancelled;
        LastError = Truncate(reason, ErrorMaxLength);
        CancelledAtUtc = DateTime.UtcNow;
        NextRetryAtUtc = null;
        LockOwnerId = null;
        LockedUntilUtc = null;
        Touch();
    }

    public void ReleaseClaim()
    {
        if (Status is BackgroundJobStatus.Running)
        {
            Status = BackgroundJobStatus.Pending;
        }

        LockOwnerId = null;
        LockedUntilUtc = null;
        Touch();
    }

    public void PrepareForManualRetry()
    {
        if (Status is not (BackgroundJobStatus.Failed or BackgroundJobStatus.DeadLetter))
        {
            throw new InvalidOperationException("Only failed or dead-letter jobs can be manually retried.");
        }

        Status = BackgroundJobStatus.Pending;
        ExecuteAtUtc = DateTime.UtcNow;
        NextRetryAtUtc = null;
        AttemptCount = 0;
        LastError = null;
        LockOwnerId = null;
        LockedUntilUtc = null;
        CompletedAtUtc = null;
        Touch();
    }

    private static void ValidateJobType(string jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType) || jobType.Length > JobTypeMaxLength)
        {
            throw new ArgumentException("Job type is required.", nameof(jobType));
        }
    }

    private static string? NormalizePayload(string? payloadJson) =>
        string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson.Trim();

    private static string? NormalizeIdempotencyKey(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
