namespace Commerce.DisasterRecovery.Domain.Enums;

public enum BackupRunStatus
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4
}

public enum BackupValidityStatus
{
    Unverified = 1,
    ChecksumVerified = 2,
    RestoreTested = 3
}

public enum BackupComponentType
{
    Database = 1,
    Media = 2,
    Downloads = 3,
    Configuration = 4,
    Plugins = 5,
    Manifest = 6
}

public enum RecoveryTestStatus
{
    InProgress = 1,
    Passed = 2,
    Failed = 3
}
