using System.Security.Cryptography;
using System.Text;
using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Domain.Enums;

namespace Commerce.DisasterRecovery.Infrastructure.Backup;

using System.IO.Compression;
using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Application.Services;
using Commerce.DisasterRecovery.Domain.Enums;
using global::Commerce.DisasterRecovery.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.DisasterRecovery.Infrastructure.Backup;

public sealed class BackupComponentCollector(
    ISqlServerDatabaseBackupProvider databaseBackupProvider,
    IDataIntegrityProbe integrityProbe,
    IOptions<DisasterRecoveryInfrastructureOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<BackupComponentCollector> logger) : IBackupComponentCollector
{
    private static readonly string[] ConfigurationFiles =
    [
        "appsettings.json",
        "appsettings.Development.json",
        "appsettings.Staging.json",
        "appsettings.Production.json",
        Path.Combine("App_Data", "commerce.database.json")
    ];

    public IReadOnlyList<BackupComponentKind> GetComponentOrder() =>
    [
        BackupComponentKind.Database,
        BackupComponentKind.Media,
        BackupComponentKind.Downloads,
        BackupComponentKind.Configuration,
        BackupComponentKind.Plugins
    ];

    public async Task<CollectedBackupComponent> CollectAsync(
        BackupComponentKind component,
        string rootPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        return component switch
        {
            BackupComponentKind.Database => await CollectDatabaseAsync(rootPath, cancellationToken).ConfigureAwait(false),
            BackupComponentKind.Media => CollectDirectory(BackupComponentType.Media, ResolvePath(options.Value.MediaRoot), rootPath, "media.zip"),
            BackupComponentKind.Downloads => CollectDirectory(BackupComponentType.Downloads, ResolvePath(Path.Combine("App_Data", "downloads")), rootPath, "downloads.zip"),
            BackupComponentKind.Configuration => await CollectConfigurationAsync(rootPath, cancellationToken).ConfigureAwait(false),
            BackupComponentKind.Plugins => CollectDirectory(BackupComponentType.Plugins, ResolvePath(options.Value.PluginsRoot), rootPath, "plugins.zip"),
            _ => throw new ArgumentOutOfRangeException(nameof(component), component, "Unsupported backup component.")
        };
    }

    public Task<IntegritySnapshot> CaptureIntegritySnapshotAsync(CancellationToken cancellationToken = default) =>
        CaptureSnapshotCoreAsync(cancellationToken);

    private async Task<IntegritySnapshot> CaptureSnapshotCoreAsync(CancellationToken cancellationToken)
    {
        var live = await integrityProbe.CaptureAsync(cancellationToken).ConfigureAwait(false);
        return new IntegritySnapshot
        {
            MediaAssetCount = live.MediaAssetCount,
            MediaFileCount = live.MediaFileCount,
            DownloadEntitlementCount = live.DownloadEntitlementCount,
            InstalledPluginCount = live.InstalledPluginCount,
            SettingsCount = live.SettingsCount,
            MigrationVersions = live.MigrationVersions,
            InstalledPlugins = live.InstalledPlugins
        };
    }

    private async Task<CollectedBackupComponent> CollectDatabaseAsync(string rootPath, CancellationToken cancellationToken)
    {
        const string relativePath = "database.bak";
        var targetPath = Path.Combine(rootPath, relativePath);

        try
        {
            await databaseBackupProvider.BackupDatabaseAsync(targetPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Database backup skipped: {Message}", ex.Message);
            return new CollectedBackupComponent(BackupComponentType.Database, string.Empty, 0, string.Empty, false, ex.Message);
        }

        if (!File.Exists(targetPath))
        {
            return new CollectedBackupComponent(
                BackupComponentType.Database,
                string.Empty,
                0,
                string.Empty,
                false,
                "SQL Server wrote the backup on the database server host; the file is not accessible from the application host.");
        }

        return new CollectedBackupComponent(
            BackupComponentType.Database,
            relativePath,
            new FileInfo(targetPath).Length,
            BackupFileHash.ComputeSha256(targetPath),
            true,
            null);
    }

    private async Task<CollectedBackupComponent> CollectConfigurationAsync(string rootPath, CancellationToken cancellationToken)
    {
        const string relativePath = "configuration.zip";
        var targetPath = Path.Combine(rootPath, relativePath);

        var existingFiles = ConfigurationFiles
            .Select(file => (RelativeName: file.Replace('\\', '/'), FullPath: ResolvePath(file)))
            .Where(x => File.Exists(x.FullPath))
            .ToList();

        if (existingFiles.Count == 0)
        {
            return new CollectedBackupComponent(BackupComponentType.Configuration, string.Empty, 0, string.Empty, false, "No configuration files found.");
        }

        await using (var zipStream = File.Create(targetPath))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            foreach (var (relativeName, fullPath) in existingFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
                if (options.Value.MaskSecretsInConfigurationBackup)
                {
                    content = SecretMasker.MaskConnectionStringSecrets(content);
                }

                var entry = archive.CreateEntry(relativeName);
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream);
                await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }

        return new CollectedBackupComponent(
            BackupComponentType.Configuration,
            relativePath,
            new FileInfo(targetPath).Length,
            BackupFileHash.ComputeSha256(targetPath),
            true,
            null);
    }

    private CollectedBackupComponent CollectDirectory(
        BackupComponentType componentType,
        string sourceDirectory,
        string rootPath,
        string archiveFileName)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            logger.LogInformation("Backup component {Component} skipped; directory {Directory} does not exist.", componentType, sourceDirectory);
            return new CollectedBackupComponent(componentType, string.Empty, 0, string.Empty, false, $"Directory '{sourceDirectory}' does not exist.");
        }

        var targetPath = Path.Combine(rootPath, archiveFileName);
        ZipFile.CreateFromDirectory(sourceDirectory, targetPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        return new CollectedBackupComponent(
            componentType,
            archiveFileName,
            new FileInfo(targetPath).Length,
            BackupFileHash.ComputeSha256(targetPath),
            true,
            null);
    }

    private string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(hostEnvironment.ContentRootPath, path);
}


//public sealed class BackupComponentCollector : IBackupComponentCollector
//{
//    private readonly ISqlServerDatabaseBackupProvider _dbProvider;
//    private readonly DisasterRecoveryInfrastructureOptions _options;

//    public BackupComponentCollector(ISqlServerDatabaseBackupProvider dbProvider, Microsoft.Extensions.Options.IOptions<DisasterRecoveryInfrastructureOptions> options)
//    {
//        _dbProvider = dbProvider;
//        _options = options.Value;
//    }

//    public IReadOnlyList<Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind> GetComponentOrder()
//    {
//        return new[]
//        {
//            Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind.Database,
//            Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind.Media,
//            Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind.Downloads,
//            Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind.Configuration,
//            Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind.Plugins
//        };
//    }

//    public async Task<Commerce.DisasterRecovery.Application.Abstractions.CollectedBackupComponent> CollectAsync(Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind component, string rootPath, CancellationToken cancellationToken = default)
//    {
//        switch (component)
//        {
//            case Commerce.DisasterRecovery.Application.Abstractions.BackupComponentKind.Database:
//            {
//                var dbFile = await _dbProvider.BackupDatabaseAsync(rootPath, cancellationToken).ConfigureAwait(false);
//                var fi = new FileInfo(dbFile);
//                var sha = ComputeSha256(dbFile);
//                return new Commerce.DisasterRecovery.Application.Abstractions.CollectedBackupComponent(
//                    BackupComponentType.Database,
//                    RelativePath: Path.GetFileName(dbFile),
//                    SizeBytes: fi.Length,
//                    Sha256: sha,
//                    Included: true,
//                    Message: null);
//            }
//            default:
//                // For other components create empty placeholders to ensure the flow works.
//                var placeholder = Path.Combine(rootPath, component.ToString().ToLowerInvariant());
//                Directory.CreateDirectory(placeholder);
//                return new Commerce.DisasterRecovery.Application.Abstractions.CollectedBackupComponent(
//                    BackupComponentType.Manifest,
//                    RelativePath: Path.GetFileName(placeholder),
//                    SizeBytes: 0,
//                    Sha256: string.Empty,
//                    Included: false,
//                    Message: "Not implemented in this environment");
//        }
//    }

//    public Task<Commerce.DisasterRecovery.Application.Services.IntegritySnapshot> CaptureIntegritySnapshotAsync(CancellationToken cancellationToken = default)
//    {
//        var snapshot = new Commerce.DisasterRecovery.Application.Services.IntegritySnapshot
//        {
//            MediaAssetCount = 0,
//            MediaFileCount = 0,
//            DownloadEntitlementCount = 0,
//            InstalledPluginCount = 0,
//            SettingsCount = 0,
//            MigrationVersions = Array.Empty<string>(),
//            InstalledPlugins = Array.Empty<string>()
//        };

//        return Task.FromResult(snapshot);
//    }

//    private static string ComputeSha256(string path)
//    {
//        using var stream = File.OpenRead(path);
//        var hash = SHA256.HashData(stream);
//        return Convert.ToHexString(hash).ToLowerInvariant();
//    }
//}
