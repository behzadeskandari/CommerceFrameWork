namespace Commerce.DisasterRecovery.Contracts;

public enum BackupComponentType
{
    Database = 1,
    Media = 2,
    Downloads = 3,
    Configuration = 4,
    Plugins = 5,
    Manifest = 6
}

public enum BackupRunStatus
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4
}

/// <summary>
/// Backups are not considered valid for production recovery until <see cref="RestoreTested"/>.
/// </summary>
public enum BackupValidityStatus
{
    Unverified = 1,
    ChecksumVerified = 2,
    RestoreTested = 3
}

public enum RecoveryTestStatus
{
    InProgress = 1,
    Passed = 2,
    Failed = 3
}

public sealed record BackupComponentDto(
    BackupComponentType ComponentType,
    string RelativePath,
    long SizeBytes,
    string Sha256,
    bool Included,
    string? Message);

public sealed record BackupRunDto(
    long Id,
    string BackupKey,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    BackupRunStatus Status,
    BackupValidityStatus ValidityStatus,
    bool IsValidForRecovery,
    string? RootPath,
    string? FailureMessage,
    IReadOnlyList<BackupComponentDto> Components);

public sealed record RecoveryTestDto(
    long Id,
    int BackupRunId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    RecoveryTestStatus Status,
    bool DatabaseVerifyOnlyPassed,
    bool FileRecoveryPassed,
    string? Message);

public sealed record DataIntegrityReportDto(
    DateTime GeneratedAtUtc,
    int MediaAssetCount,
    int MediaFileCount,
    int DownloadEntitlementCount,
    int InstalledPluginCount,
    int SettingsCount,
    bool MediaFilesMatchDatabase,
    bool PluginFoldersMatchDatabase,
    IReadOnlyList<string> Warnings);

public sealed record DisasterRecoveryTargetsDto(
    TimeSpan RecoveryPointObjective,
    TimeSpan RecoveryTimeObjective,
    string Description);

public interface IBackupService
{
    Task<Result<BackupRunDto>> CreateBackupAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<BackupRunDto>>> ListBackupsAsync(CancellationToken cancellationToken = default);

    Task<Result<BackupRunDto>> GetBackupAsync(int backupRunId, CancellationToken cancellationToken = default);

    Task<Result> ApplyRetentionPolicyAsync(CancellationToken cancellationToken = default);
}

public interface IBackupVerificationService
{
    Task<Result<BackupRunDto>> VerifyChecksumsAsync(int backupRunId, CancellationToken cancellationToken = default);
}

public interface IRecoveryTestService
{
    Task<Result<RecoveryTestDto>> RunRecoveryTestAsync(int backupRunId, CancellationToken cancellationToken = default);
}

public interface IDataIntegrityService
{
    Task<Result<DataIntegrityReportDto>> GetLiveIntegrityReportAsync(CancellationToken cancellationToken = default);
}

public interface IDisasterRecoveryMetadataService
{
    DisasterRecoveryTargetsDto GetTargets();
}
