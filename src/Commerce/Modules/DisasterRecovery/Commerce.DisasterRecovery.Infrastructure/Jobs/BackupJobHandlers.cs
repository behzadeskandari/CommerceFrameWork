using Commerce.DisasterRecovery.Contracts;
using Commerce.Framework.Scheduling;
using Microsoft.Extensions.Logging;

namespace Commerce.DisasterRecovery.Infrastructure.Jobs;

public sealed class BackupCreateJobHandler(
    IBackupService backupService,
    ILogger<BackupCreateJobHandler> logger) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.BackupCreate;

    public async Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await backupService.CreateBackupAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            logger.LogError("Scheduled backup failed: {Message}", result.Error?.Message);
            return new BackgroundJobHandleResult(false, result.Error?.Message);
        }

        logger.LogInformation("Scheduled backup {BackupKey} completed with status {Status}. Valid for recovery: {Valid}.",
            result.Value!.BackupKey,
            result.Value.Status,
            result.Value.IsValidForRecovery);
        return new BackgroundJobHandleResult(true);
    }
}

public sealed class BackupRetentionJobHandler(
    IBackupService backupService,
    ILogger<BackupRetentionJobHandler> logger) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.BackupRetention;

    public async Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await backupService.ApplyRetentionPolicyAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            logger.LogError("Backup retention failed: {Message}", result.Error?.Message);
            return new BackgroundJobHandleResult(false, result.Error?.Message);
        }

        logger.LogInformation("Backup retention policy applied.");
        return new BackgroundJobHandleResult(true);
    }
}
