using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.PluginContracts.Discovery;

public interface IPluginDiscoveryService
{
    IReadOnlyList<DiscoveredPlugin> Discover();

    DiscoveredPlugin? FindBySystemName(string systemName);
}

public sealed record DiscoveredPlugin(
    PluginManifest Manifest,
    PluginDescriptor Descriptor,
    string ManifestPath,
    string AssemblyPath);
