using Commerce.DisasterRecovery.Application.Services;
using Commerce.DisasterRecovery.Domain.Entities;
using DomainBackupComponentType = Commerce.DisasterRecovery.Domain.Enums.BackupComponentType;

namespace Commerce.DisasterRecovery.Application.Abstractions;

public enum BackupComponentKind
{
    Database,
    Media,
    Downloads,
    Configuration,
    Plugins
}

public sealed record CollectedBackupComponent(
    DomainBackupComponentType ComponentType,
    string RelativePath,
    long SizeBytes,
    string Sha256,
    bool Included,
    string? Message);

public interface IBackupRepository
{
    Task AddAsync(BackupRun run, CancellationToken cancellationToken = default);

    Task AddRecoveryTestAsync(RecoveryTestRun test, CancellationToken cancellationToken = default);

    Task<BackupRun?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupRun>> ListAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(BackupRun run, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IBackupComponentCollector
{
    IReadOnlyList<BackupComponentKind> GetComponentOrder();

    Task<CollectedBackupComponent> CollectAsync(BackupComponentKind component, string rootPath, CancellationToken cancellationToken = default);

    Task<IntegritySnapshot> CaptureIntegritySnapshotAsync(CancellationToken cancellationToken = default);
}

public interface ISqlServerBackupVerifier
{
    Task<bool> VerifyOnlyAsync(string backupFilePath, CancellationToken cancellationToken = default);
}

public sealed record LiveIntegrityProbeSnapshot(
    int MediaAssetCount,
    int MediaFileCount,
    int DownloadEntitlementCount,
    int InstalledPluginCount,
    int PluginFolderCount,
    int SettingsCount,
    IReadOnlyList<string> MigrationVersions,
    IReadOnlyList<string> InstalledPlugins);

public interface IDataIntegrityProbe
{
    Task<LiveIntegrityProbeSnapshot> CaptureAsync(CancellationToken cancellationToken = default);
}
