namespace Commerce.Framework.PluginContracts.Plugins;

public sealed record PluginDependency(
    string PluginSystemName,
    string? MinimumVersion = null,
    string? MaximumVersion = null);
