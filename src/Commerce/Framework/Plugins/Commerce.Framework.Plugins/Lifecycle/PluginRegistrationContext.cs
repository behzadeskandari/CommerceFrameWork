using Commerce.Framework.PluginContracts.Lifecycle;
using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.Plugins.Lifecycle;

public sealed class PluginRegistrationContext : IPluginRegistrationContext
{
    private readonly Dictionary<string, LoadedPluginEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public PluginRegistrationContext(
        IReadOnlyList<LoadedPluginEntry> plugins,
        IReadOnlyList<PluginDescriptor> orderedDescriptors)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        ArgumentNullException.ThrowIfNull(orderedDescriptors);

        Plugins = plugins;
        OrderedDescriptors = orderedDescriptors;

        foreach (var plugin in plugins)
        {
            _entries[plugin.Descriptor.SystemName] = plugin;
        }
    }

    public IReadOnlyList<LoadedPluginEntry> Plugins { get; }

    public IReadOnlyList<PluginDescriptor> OrderedDescriptors { get; }

    public IReadOnlyList<string> OrderedSystemNames =>
        OrderedDescriptors.Select(x => x.SystemName).ToList();

    public LoadedPluginEntry GetEntry(string systemName) => _entries[systemName];
}
