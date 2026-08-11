namespace Commerce.Framework.Contracts.Installation;

public interface IInstallationStateService
{
    Task<InstallationStateInfo> GetStateAsync(CancellationToken cancellationToken = default);

    Task<bool> IsInstalledAsync(CancellationToken cancellationToken = default);

    Task<bool> IsInstallationLockedAsync(CancellationToken cancellationToken = default);
}
