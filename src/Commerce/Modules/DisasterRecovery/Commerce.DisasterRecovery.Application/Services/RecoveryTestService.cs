using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Application.Mapping;
using Commerce.DisasterRecovery.Contracts;
using Commerce.DisasterRecovery.Domain.Entities;
using Commerce.Framework.Core.Errors;
using DomainEnums = Commerce.DisasterRecovery.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.DisasterRecovery.Application.Services;

public sealed class RecoveryTestService(
    IBackupRepository repository,
    ISqlServerBackupVerifier sqlServerBackupVerifier,
    IOptions<DisasterRecoveryApplicationOptions> options,
    ILogger<RecoveryTestService> logger) : IRecoveryTestService
{
    public async Task<Result<RecoveryTestDto>> RunRecoveryTestAsync(int backupRunId, CancellationToken cancellationToken = default)
    {
        var run = await repository.GetByIdAsync(backupRunId, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            return Result.Failure<RecoveryTestDto>(Error.NotFound($"Backup run '{backupRunId}' was not found."));
        }

        if (run.Status is not (DomainEnums.BackupRunStatus.Completed or DomainEnums.BackupRunStatus.PartiallyCompleted))
        {
            return Result.Failure<RecoveryTestDto>(Error.Validation("Recovery tests can only run against completed backups."));
        }

        var test = RecoveryTestRun.Start(backupRunId);
        await repository.AddRecoveryTestAsync(test, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var databaseArtifact = run.Artifacts.FirstOrDefault(x => x.ComponentType == DomainEnums.BackupComponentType.Database);
        var databasePassed = true;
        if (databaseArtifact is { Included: true })
        {
            var databasePath = Path.Combine(run.RootPath, databaseArtifact.RelativePath);
            databasePassed = await sqlServerBackupVerifier.VerifyOnlyAsync(databasePath, cancellationToken).ConfigureAwait(false);
        }
        else if (databaseArtifact is { Included: false })
        {
            databasePassed = false;
        }

        var stagingRoot = Path.Combine(options.Value.BackupRoot, "_recovery-tests", run.BackupKey);
        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, recursive: true);
        }

        Directory.CreateDirectory(stagingRoot);
        var fileRecoveryPassed = true;
        try
        {
            foreach (var artifact in run.Artifacts.Where(x => x.Included && x.ComponentType is DomainEnums.BackupComponentType.Media or DomainEnums.BackupComponentType.Plugins or DomainEnums.BackupComponentType.Configuration))
            {
                var source = Path.Combine(run.RootPath, artifact.RelativePath);
                var destination = Path.Combine(stagingRoot, artifact.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, overwrite: true);
            }

            foreach (var artifact in run.Artifacts.Where(x => x.Included && x.ComponentType is DomainEnums.BackupComponentType.Media or DomainEnums.BackupComponentType.Plugins or DomainEnums.BackupComponentType.Configuration))
            {
                var restored = Path.Combine(stagingRoot, artifact.RelativePath);
                if (!File.Exists(restored))
                {
                    fileRecoveryPassed = false;
                    break;
                }

                var hash = BackupService.ComputeSha256(restored);
                if (!hash.Equals(artifact.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    fileRecoveryPassed = false;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "File recovery test failed for backup {BackupKey}.", run.BackupKey);
            fileRecoveryPassed = false;
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }

        var passed = databasePassed && fileRecoveryPassed && databaseArtifact is { Included: true };
        var message = passed
            ? "Recovery test passed. Backup is valid for recovery."
            : databaseArtifact is not { Included: true }
                ? "Recovery test incomplete: database backup was not included. File components may be verified separately, but this backup is not valid for full recovery."
                : $"Recovery test failed. Database verify: {databasePassed}; file recovery: {fileRecoveryPassed}.";

        test.Complete(
            passed ? DomainEnums.RecoveryTestStatus.Passed : DomainEnums.RecoveryTestStatus.Failed,
            databasePassed,
            fileRecoveryPassed,
            message);

        if (passed)
        {
            run.MarkRestoreTested();
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(BackupMapper.ToRecoveryTestDto(test));
    }
}
