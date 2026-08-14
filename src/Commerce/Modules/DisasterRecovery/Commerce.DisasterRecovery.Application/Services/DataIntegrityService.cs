using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Contracts;

namespace Commerce.DisasterRecovery.Application.Services;

public sealed class DataIntegrityService(IDataIntegrityProbe probe) : IDataIntegrityService
{
    public async Task<Result<DataIntegrityReportDto>> GetLiveIntegrityReportAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await probe.CaptureAsync(cancellationToken).ConfigureAwait(false);
        var warnings = new List<string>();

        if (snapshot.MediaAssetCount != snapshot.MediaFileCount)
        {
            warnings.Add($"Media asset count ({snapshot.MediaAssetCount}) does not match media file count ({snapshot.MediaFileCount}).");
        }

        if (snapshot.InstalledPluginCount != snapshot.PluginFolderCount)
        {
            warnings.Add($"Installed plugin count ({snapshot.InstalledPluginCount}) does not match plugin folder count ({snapshot.PluginFolderCount}).");
        }

        return Result.Success(new DataIntegrityReportDto(
            DateTime.UtcNow,
            snapshot.MediaAssetCount,
            snapshot.MediaFileCount,
            snapshot.DownloadEntitlementCount,
            snapshot.InstalledPluginCount,
            snapshot.SettingsCount,
            snapshot.MediaAssetCount == snapshot.MediaFileCount,
            snapshot.InstalledPluginCount == snapshot.PluginFolderCount,
            warnings));
    }
}

public sealed class DisasterRecoveryMetadataService : IDisasterRecoveryMetadataService
{
    public DisasterRecoveryTargetsDto GetTargets() =>
        new(
            RecoveryPointObjective: TimeSpan.FromHours(24),
            RecoveryTimeObjective: TimeSpan.FromHours(4),
            Description: "Default targets assume daily backups with a four-hour full-restore runbook. Tune Commerce:DisasterRecovery schedule and infrastructure to meet your SLA.");
}
