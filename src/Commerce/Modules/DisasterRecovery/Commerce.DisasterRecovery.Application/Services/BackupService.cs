using System.Security.Cryptography;
using System.Text.Json;
using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Application.Mapping;
using Commerce.DisasterRecovery.Contracts;
using Commerce.DisasterRecovery.Domain.Entities;
using Commerce.DisasterRecovery.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.DisasterRecovery.Application.Services;

public sealed class BackupService(
    IBackupRepository repository,
    IBackupComponentCollector componentCollector,
    IOptions<DisasterRecoveryApplicationOptions> options,
    ILogger<BackupService> logger) : IBackupService
{
    public async Task<Result<BackupRunDto>> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        var backupKey = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var rootPath = Path.Combine(options.Value.BackupRoot, backupKey);
        Directory.CreateDirectory(rootPath);

        var run = BackupRun.Start(backupKey, rootPath);
        await repository.AddAsync(run, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var failures = new List<string>();
        var artifacts = new List<BackupArtifact>();

        foreach (var component in componentCollector.GetComponentOrder())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var collected = await componentCollector.CollectAsync(component, rootPath, cancellationToken)
                    .ConfigureAwait(false);
                artifacts.Add(BackupArtifact.Create(
                    run.Id,
                    collected.ComponentType,
                    collected.RelativePath,
                    collected.SizeBytes,
                    collected.Sha256,
                    collected.Included,
                    collected.Message));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Backup component {Component} failed.", component);
                failures.Add($"{component}: {ex.Message}");
                artifacts.Add(BackupArtifact.Create(
                    run.Id,
                    MapComponentType(component),
                    string.Empty,
                    0,
                    string.Empty,
                    false,
                    ex.Message));
            }
        }

        foreach (var artifact in artifacts)
        {
            run.Artifacts.Add(artifact);
        }

        var integrity = await componentCollector.CaptureIntegritySnapshotAsync(cancellationToken).ConfigureAwait(false);
        var manifestPath = Path.Combine(rootPath, "backup-manifest.json");
        var manifest = new BackupManifestDocument
        {
            BackupKey = backupKey,
            CreatedAtUtc = run.StartedAtUtc,
            Integrity = integrity,
            Components = artifacts.Select(a => new BackupManifestComponent
            {
                ComponentType = a.ComponentType.ToString(),
                RelativePath = a.RelativePath,
                SizeBytes = a.SizeBytes,
                Sha256 = a.Sha256,
                Included = a.Included,
                Message = a.Message
            }).ToList()
        };

        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
        var manifestHash = BackupFileHash.ComputeSha256(manifestPath);
        run.Artifacts.Add(BackupArtifact.Create(
            run.Id,
            Domain.Enums.BackupComponentType.Manifest,
            "backup-manifest.json",
            new FileInfo(manifestPath).Length,
            manifestHash,
            true,
            null));

        var status = failures.Count == 0
            ? Domain.Enums.BackupRunStatus.Completed
            : artifacts.Any(a => a.Included)
                ? Domain.Enums.BackupRunStatus.PartiallyCompleted
                : Domain.Enums.BackupRunStatus.Failed;

        run.Complete(status, "backup-manifest.json", JsonSerializer.Serialize(integrity, JsonOptions), failures.Count > 0 ? string.Join("; ", failures) : null);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = BackupMapper.ToDto(run);
        return status == Domain.Enums.BackupRunStatus.Failed
            ? Result.Failure<BackupRunDto>(Error.Validation($"Backup failed: {run.FailureMessage}"))
            : Result.Success(dto);
    }

    public async Task<Result<IReadOnlyList<BackupRunDto>>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var runs = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<BackupRunDto>>(runs.Select(BackupMapper.ToDto).ToList());
    }

    public async Task<Result<BackupRunDto>> GetBackupAsync(int backupRunId, CancellationToken cancellationToken = default)
    {
        var run = await repository.GetByIdAsync(backupRunId, cancellationToken).ConfigureAwait(false);
        return run is null
            ? Result.Failure<BackupRunDto>(Error.NotFound($"Backup run '{backupRunId}' was not found."))
            : Result.Success(BackupMapper.ToDto(run));
    }

    public async Task<Result> ApplyRetentionPolicyAsync(CancellationToken cancellationToken = default)
    {
        var runs = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        var ordered = runs.OrderByDescending(x => x.StartedAtUtc).ToList();
        var cutoff = DateTime.UtcNow.AddDays(-options.Value.RetentionDays);
        var toDelete = ordered
            .Skip(options.Value.MinBackupsToKeep)
            .Where(x => x.StartedAtUtc < cutoff)
            .ToList();

        foreach (var run in toDelete)
        {
            if (Directory.Exists(run.RootPath))
            {
                Directory.Delete(run.RootPath, recursive: true);
            }

            await repository.DeleteAsync(run, cancellationToken).ConfigureAwait(false);
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static Domain.Enums.BackupComponentType MapComponentType(BackupComponentKind kind) => kind switch
    {
        BackupComponentKind.Database => Domain.Enums.BackupComponentType.Database,
        BackupComponentKind.Media => Domain.Enums.BackupComponentType.Media,
        BackupComponentKind.Downloads => Domain.Enums.BackupComponentType.Downloads,
        BackupComponentKind.Configuration => Domain.Enums.BackupComponentType.Configuration,
        BackupComponentKind.Plugins => Domain.Enums.BackupComponentType.Plugins,
        _ => Domain.Enums.BackupComponentType.Manifest
    };

    internal static string ComputeSha256(string path) => BackupFileHash.ComputeSha256(path);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

public sealed class DisasterRecoveryApplicationOptions
{
    public const string SectionName = "Commerce:DisasterRecovery";

    public string BackupRoot { get; set; } = "App_Data/backups";

    public int RetentionDays { get; set; } = 30;

    public int MinBackupsToKeep { get; set; } = 7;
}

public sealed class BackupManifestDocument
{
    public required string BackupKey { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required IntegritySnapshot Integrity { get; init; }

    public required List<BackupManifestComponent> Components { get; init; }
}

public sealed class BackupManifestComponent
{
    public required string ComponentType { get; init; }

    public required string RelativePath { get; init; }

    public long SizeBytes { get; init; }

    public required string Sha256 { get; init; }

    public bool Included { get; init; }

    public string? Message { get; init; }
}

public sealed class IntegritySnapshot
{
    public int MediaAssetCount { get; init; }

    public int MediaFileCount { get; init; }

    public int DownloadEntitlementCount { get; init; }

    public int InstalledPluginCount { get; init; }

    public int SettingsCount { get; init; }

    public IReadOnlyList<string> MigrationVersions { get; init; } = [];

    public IReadOnlyList<string> InstalledPlugins { get; init; } = [];
}
