using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.PluginContracts.Lifecycle;

public interface IPluginRegistrationContext
{
    IReadOnlyList<LoadedPluginEntry> Plugins { get; }

    IReadOnlyList<PluginDescriptor> OrderedDescriptors { get; }

    IReadOnlyList<string> OrderedSystemNames { get; }

    LoadedPluginEntry GetEntry(string systemName);
}

public sealed class LoadedPluginEntry
{
    public LoadedPluginEntry(
        PluginDescriptor descriptor,
        ICommercePlugin plugin,
        PluginState state)
    {
        Descriptor = descriptor;
        Plugin = plugin;
        State = state;
    }

    public PluginDescriptor Descriptor { get; }

    public ICommercePlugin Plugin { get; }

    public PluginState State { get; set; }

    public TimeSpan? StartupDuration { get; set; }

    public string? FailureReason { get; set; }

    public bool IsInstalled { get; set; }

    public bool IsEnabled { get; set; }
}
