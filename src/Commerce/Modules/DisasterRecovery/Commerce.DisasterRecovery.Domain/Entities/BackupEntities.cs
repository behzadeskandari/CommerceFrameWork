using Commerce.DisasterRecovery.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.DisasterRecovery.Domain.Entities;

public sealed class BackupRun : Entity
{
    private BackupRun()
    {
    }

    public string BackupKey { get; private set; } = string.Empty;

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public BackupRunStatus Status { get; private set; }

    public BackupValidityStatus ValidityStatus { get; private set; }

    public string RootPath { get; private set; } = string.Empty;

    public string? ManifestRelativePath { get; private set; }

    public string? FailureMessage { get; private set; }

    public string IntegritySnapshotJson { get; private set; } = "{}";

    public ICollection<BackupArtifact> Artifacts { get; private set; } = [];

    public ICollection<RecoveryTestRun> RecoveryTests { get; private set; } = [];

    public static BackupRun Start(string backupKey, string rootPath) =>
        new()
        {
            BackupKey = backupKey,
            StartedAtUtc = DateTime.UtcNow,
            Status = BackupRunStatus.InProgress,
            ValidityStatus = BackupValidityStatus.Unverified,
            RootPath = rootPath
        };

    public void Complete(BackupRunStatus status, string? manifestRelativePath, string integritySnapshotJson, string? failureMessage = null)
    {
        CompletedAtUtc = DateTime.UtcNow;
        Status = status;
        ManifestRelativePath = manifestRelativePath;
        IntegritySnapshotJson = integritySnapshotJson;
        FailureMessage = failureMessage;
        ValidityStatus = BackupValidityStatus.Unverified;
    }

    public void MarkChecksumVerified() => ValidityStatus = BackupValidityStatus.ChecksumVerified;

    public void MarkRestoreTested() => ValidityStatus = BackupValidityStatus.RestoreTested;

    public bool IsValidForRecovery => ValidityStatus == BackupValidityStatus.RestoreTested && Status == BackupRunStatus.Completed;
}

public sealed class BackupArtifact : Entity
{
    private BackupArtifact()
    {
    }

    public int BackupRunId { get; private set; }

    public BackupComponentType ComponentType { get; private set; }

    public string RelativePath { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string Sha256 { get; private set; } = string.Empty;

    public bool Included { get; private set; }

    public string? Message { get; private set; }

    public BackupRun? BackupRun { get; private set; }

    public static BackupArtifact Create(
        int backupRunId,
        BackupComponentType componentType,
        string relativePath,
        long sizeBytes,
        string sha256,
        bool included,
        string? message) =>
        new()
        {
            BackupRunId = backupRunId,
            ComponentType = componentType,
            RelativePath = relativePath,
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            Included = included,
            Message = message
        };
}

public sealed class RecoveryTestRun : Entity
{
    private RecoveryTestRun()
    {
    }

    public int BackupRunId { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public RecoveryTestStatus Status { get; private set; }

    public bool DatabaseVerifyOnlyPassed { get; private set; }

    public bool FileRecoveryPassed { get; private set; }

    public string? Message { get; private set; }

    public BackupRun? BackupRun { get; private set; }

    public static RecoveryTestRun Start(int backupRunId) =>
        new()
        {
            BackupRunId = backupRunId,
            StartedAtUtc = DateTime.UtcNow,
            Status = RecoveryTestStatus.InProgress
        };

    public void Complete(RecoveryTestStatus status, bool databaseVerifyOnlyPassed, bool fileRecoveryPassed, string? message)
    {
        CompletedAtUtc = DateTime.UtcNow;
        Status = status;
        DatabaseVerifyOnlyPassed = databaseVerifyOnlyPassed;
        FileRecoveryPassed = fileRecoveryPassed;
        Message = message;
    }
}
