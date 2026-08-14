using Commerce.DisasterRecovery.Contracts;
using Commerce.DisasterRecovery.Infrastructure.Backup;
using Commerce.Framework.Contracts.Observability;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.DisasterRecovery.Infrastructure.Health;

public sealed class BackupHealthProbe(
    IServiceScopeFactory scopeFactory,
    IOptions<DisasterRecoveryInfrastructureOptions> options) : IBackupHealthProbe
{
    public async Task<BackupHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var backups = await backupService.ListBackupsAsync(cancellationToken).ConfigureAwait(false);
        if (backups.IsFailure)
        {
            return new BackupHealthSnapshot(false, false, null, null, backups.Error?.Message);
        }

        var latest = backups.Value!.OrderByDescending(x => x.StartedAtUtc).FirstOrDefault();
        if (latest is null)
        {
            return new BackupHealthSnapshot(false, false, null, null, "No backups have been created.");
        }

        var maxBackupAge = TimeSpan.FromHours(options.Value.MaxBackupAgeHoursBeforeAlert);
        var backupFresh = DateTime.UtcNow - latest.StartedAtUtc <= maxBackupAge;
        var restoreTested = latest.IsValidForRecovery;
        var message = restoreTested
            ? "Latest backup passed recovery testing."
            : latest.ValidityStatus == BackupValidityStatus.ChecksumVerified
                ? "Latest backup is checksum-verified but has not passed recovery testing."
                : "Latest backup has not been verified for recovery.";

        if (!backupFresh)
        {
            message = $"Latest backup is older than {maxBackupAge.TotalHours:0} hours.";
        }

        return new BackupHealthSnapshot(backupFresh, restoreTested, latest.StartedAtUtc, latest.ValidityStatus.ToString(), message);
    }
}
