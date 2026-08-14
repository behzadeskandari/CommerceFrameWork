namespace Commerce.Framework.PluginContracts.Plugins;

public sealed record PluginDescriptor(
    string Id,
    string SystemName,
    string Name,
    Version Version,
    string Author,
    string Description,
    string? Website,
    IReadOnlyList<PluginDependency> Dependencies,
    Version? MinimumCommerceVersion,
    Version? MaximumCommerceVersion,
    bool IsSystemPlugin,
    bool IsRequired,
    string AssemblyName,
    string PluginDirectory);
