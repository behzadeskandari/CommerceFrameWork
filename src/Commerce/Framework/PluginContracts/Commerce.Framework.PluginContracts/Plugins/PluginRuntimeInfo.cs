namespace Commerce.Framework.PluginContracts.Plugins;

public sealed record PluginRuntimeInfo(
    PluginDescriptor Descriptor,
    PluginState State,
    TimeSpan? StartupDuration = null,
    string? FailureReason = null,
    bool IsInstalled = false,
    bool IsEnabled = false);
