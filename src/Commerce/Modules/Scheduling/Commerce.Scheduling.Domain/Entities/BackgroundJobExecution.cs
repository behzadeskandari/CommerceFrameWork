using Commerce.Framework.Core.Entities;
using Commerce.Framework.Scheduling;

namespace Commerce.Scheduling.Domain.Entities;

public sealed class BackgroundJobExecution : AggregateRoot
{
    public const int ErrorMaxLength = 2000;

    public int JobId { get; private set; }

    public int AttemptNumber { get; private set; }

    public BackgroundJobExecutionStatus Status { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? ErrorMessage { get; private set; }

    public static BackgroundJobExecution Start(int jobId, int attemptNumber)
    {
        return new BackgroundJobExecution
        {
            JobId = jobId,
            AttemptNumber = attemptNumber,
            Status = BackgroundJobExecutionStatus.Running,
            StartedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkCompleted()
    {
        Status = BackgroundJobExecutionStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string error)
    {
        Status = BackgroundJobExecutionStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = error.Length <= ErrorMaxLength ? error : error[..ErrorMaxLength];
    }

    public void MarkCancelled(string reason)
    {
        Status = BackgroundJobExecutionStatus.Cancelled;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = reason.Length <= ErrorMaxLength ? reason : reason[..ErrorMaxLength];
    }
}
