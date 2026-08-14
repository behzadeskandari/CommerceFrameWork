using Commerce.Framework.PluginContracts.Lifecycle;
using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.PluginContracts.Lifecycle;

public interface ICommercePluginManager
{
    IReadOnlyList<PluginRuntimeInfo> Discover();

    void Validate();

    void Load();

    void Register();

    Task InstallAsync(string systemName, CancellationToken cancellationToken = default);

    Task EnableAsync(string systemName, CancellationToken cancellationToken = default);

    Task DisableAsync(string systemName, CancellationToken cancellationToken = default);

    Task UninstallAsync(
        string systemName,
        PluginUninstallMode uninstallMode = PluginUninstallMode.KeepData,
        CancellationToken cancellationToken = default);

    Task ReloadAsync(string systemName, CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
