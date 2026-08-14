using Commerce.Framework.Core.Entities;

namespace Commerce.Scheduling.Domain.Entities;

public sealed class RecurringJobSchedule : AggregateRoot
{
    public const int ScheduleKeyMaxLength = 128;
    public const int JobTypeMaxLength = 128;
    public const int PayloadMaxLength = 16000;

    public string ScheduleKey { get; private set; } = string.Empty;

    public string JobType { get; private set; } = string.Empty;

    public string? PayloadJson { get; private set; }

    public int IntervalSeconds { get; private set; }

    public int MaxAttempts { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime NextRunAtUtc { get; private set; }

    public DateTime? LastRunAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static RecurringJobSchedule Create(
        string scheduleKey,
        string jobType,
        int intervalSeconds,
        string? payloadJson,
        int maxAttempts,
        bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(scheduleKey) || scheduleKey.Length > ScheduleKeyMaxLength)
        {
            throw new ArgumentException("Schedule key is required.", nameof(scheduleKey));
        }

        if (string.IsNullOrWhiteSpace(jobType) || jobType.Length > JobTypeMaxLength)
        {
            throw new ArgumentException("Job type is required.", nameof(jobType));
        }

        if (intervalSeconds < 5)
        {
            throw new ArgumentException("Interval must be at least 5 seconds.", nameof(intervalSeconds));
        }

        var utcNow = DateTime.UtcNow;
        return new RecurringJobSchedule
        {
            ScheduleKey = scheduleKey.Trim().ToLowerInvariant(),
            JobType = jobType.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson.Trim(),
            IntervalSeconds = intervalSeconds,
            MaxAttempts = Math.Max(1, maxAttempts),
            IsEnabled = isEnabled,
            NextRunAtUtc = utcNow,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public bool IsDue(DateTime utcNow) => IsEnabled && NextRunAtUtc <= utcNow;

    public void MarkEnqueued(DateTime utcNow)
    {
        LastRunAtUtc = utcNow;
        NextRunAtUtc = utcNow.AddSeconds(IntervalSeconds);
        Touch();
    }

    public void Enable()
    {
        IsEnabled = true;
        Touch();
    }

    public void Disable()
    {
        IsEnabled = false;
        Touch();
    }

    public void UpdateInterval(int intervalSeconds)
    {
        if (intervalSeconds < 5)
        {
            throw new ArgumentException("Interval must be at least 5 seconds.", nameof(intervalSeconds));
        }

        IntervalSeconds = intervalSeconds;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
