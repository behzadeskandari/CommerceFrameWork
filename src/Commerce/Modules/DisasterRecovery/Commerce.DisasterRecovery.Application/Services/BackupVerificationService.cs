using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Application.Mapping;
using Commerce.DisasterRecovery.Contracts;
using Commerce.Framework.Core.Errors;
using DomainEnums = Commerce.DisasterRecovery.Domain.Enums;

namespace Commerce.DisasterRecovery.Application.Services;

public sealed class BackupVerificationService(IBackupRepository repository) : IBackupVerificationService
{
    public async Task<Result<BackupRunDto>> VerifyChecksumsAsync(int backupRunId, CancellationToken cancellationToken = default)
    {
        var run = await repository.GetByIdAsync(backupRunId, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            return Result.Failure<BackupRunDto>(Error.NotFound($"Backup run '{backupRunId}' was not found."));
        }

        var errors = new List<string>();
        foreach (var artifact in run.Artifacts.Where(x => x.Included && x.ComponentType != DomainEnums.BackupComponentType.Manifest))
        {
            var path = Path.Combine(run.RootPath, artifact.RelativePath);
            if (!File.Exists(path))
            {
                errors.Add($"Missing artifact: {artifact.RelativePath}");
                continue;
            }

            var hash = BackupService.ComputeSha256(path);
            if (!hash.Equals(artifact.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Checksum mismatch: {artifact.RelativePath}");
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure<BackupRunDto>(Error.Validation(string.Join("; ", errors)));
        }

        run.MarkChecksumVerified();
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(BackupMapper.ToDto(run));
    }
}
