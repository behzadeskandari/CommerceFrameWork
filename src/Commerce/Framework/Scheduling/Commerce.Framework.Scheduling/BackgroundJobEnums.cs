namespace Commerce.Framework.Scheduling;

public enum BackgroundJobKind
{
    Immediate = 1,
    Delayed = 2,
    Scheduled = 3,
    Recurring = 4
}

public enum BackgroundJobStatus
{
    Pending = 1,
    Scheduled = 2,
    Running = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6,
    DeadLetter = 7
}

public enum BackgroundJobExecutionStatus
{
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
