using Commerce.DisasterRecovery.Contracts;
using Commerce.DisasterRecovery.Domain.Entities;
using DomainBackupComponentType = Commerce.DisasterRecovery.Domain.Enums.BackupComponentType;
using DomainBackupRunStatus = Commerce.DisasterRecovery.Domain.Enums.BackupRunStatus;
using DomainBackupValidityStatus = Commerce.DisasterRecovery.Domain.Enums.BackupValidityStatus;
using DomainRecoveryTestStatus = Commerce.DisasterRecovery.Domain.Enums.RecoveryTestStatus;

namespace Commerce.DisasterRecovery.Application.Mapping;

public static class BackupMapper
{
    public static BackupRunDto ToDto(BackupRun run) =>
        new(
            run.Id,
            run.BackupKey,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            MapStatus(run.Status),
            MapValidity(run.ValidityStatus),
            run.IsValidForRecovery,
            run.RootPath,
            run.FailureMessage,
            run.Artifacts.Select(ToComponentDto).ToList());

    public static RecoveryTestDto ToRecoveryTestDto(RecoveryTestRun test) =>
        new(
            test.Id,
            test.BackupRunId,
            test.StartedAtUtc,
            test.CompletedAtUtc,
            MapRecoveryStatus(test.Status),
            test.DatabaseVerifyOnlyPassed,
            test.FileRecoveryPassed,
            test.Message);

    private static BackupComponentDto ToComponentDto(BackupArtifact artifact) =>
        new(
            MapComponent(artifact.ComponentType),
            artifact.RelativePath,
            artifact.SizeBytes,
            artifact.Sha256,
            artifact.Included,
            artifact.Message);

    private static BackupRunStatus MapStatus(DomainBackupRunStatus status) => status switch
    {
        DomainBackupRunStatus.InProgress => BackupRunStatus.InProgress,
        DomainBackupRunStatus.Completed => BackupRunStatus.Completed,
        DomainBackupRunStatus.Failed => BackupRunStatus.Failed,
        DomainBackupRunStatus.PartiallyCompleted => BackupRunStatus.PartiallyCompleted,
        _ => BackupRunStatus.Failed
    };

    private static BackupValidityStatus MapValidity(DomainBackupValidityStatus status) => status switch
    {
        DomainBackupValidityStatus.Unverified => BackupValidityStatus.Unverified,
        DomainBackupValidityStatus.ChecksumVerified => BackupValidityStatus.ChecksumVerified,
        DomainBackupValidityStatus.RestoreTested => BackupValidityStatus.RestoreTested,
        _ => BackupValidityStatus.Unverified
    };

    private static RecoveryTestStatus MapRecoveryStatus(DomainRecoveryTestStatus status) => status switch
    {
        DomainRecoveryTestStatus.InProgress => RecoveryTestStatus.InProgress,
        DomainRecoveryTestStatus.Passed => RecoveryTestStatus.Passed,
        DomainRecoveryTestStatus.Failed => RecoveryTestStatus.Failed,
        _ => RecoveryTestStatus.Failed
    };

    private static BackupComponentType MapComponent(DomainBackupComponentType type) => type switch
    {
        DomainBackupComponentType.Database => BackupComponentType.Database,
        DomainBackupComponentType.Media => BackupComponentType.Media,
        DomainBackupComponentType.Downloads => BackupComponentType.Downloads,
        DomainBackupComponentType.Configuration => BackupComponentType.Configuration,
        DomainBackupComponentType.Plugins => BackupComponentType.Plugins,
        DomainBackupComponentType.Manifest => BackupComponentType.Manifest,
        _ => BackupComponentType.Manifest
    };
}
