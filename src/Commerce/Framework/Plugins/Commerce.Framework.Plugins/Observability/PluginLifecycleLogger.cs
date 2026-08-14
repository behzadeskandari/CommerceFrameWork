using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Plugins.Observability;

public static class PluginLifecycleEvents
{
    public const string Discovered = "PluginDiscovered";
    public const string Validated = "PluginValidated";
    public const string Loaded = "PluginLoaded";
    public const string Installed = "PluginInstalled";
    public const string Enabled = "PluginEnabled";
    public const string Disabled = "PluginDisabled";
    public const string Uninstalled = "PluginUninstalled";
    public const string Failed = "PluginFailed";
    public const string ConfigurationChanged = "PluginConfigurationChanged";
    public const string Updated = "PluginUpdated";
}

public sealed class PluginLifecycleLogger(ILogger<PluginLifecycleLogger> logger)
{
    public void LogEvent(
        string operation,
        string pluginSystemName,
        string? version = null,
        bool success = true,
        string? message = null,
        string? correlationId = null)
    {
        var state = new Dictionary<string, object?>
        {
            ["PluginSystemName"] = pluginSystemName,
            ["Operation"] = operation,
            ["Result"] = success ? "Success" : "Failure",
            ["Version"] = version,
            ["CorrelationId"] = correlationId ?? Guid.NewGuid().ToString("N")
        };

        if (success)
        {
            logger.LogInformation(
                "Plugin lifecycle {Operation} for {PluginSystemName} v{Version} succeeded. {Message}",
                operation,
                pluginSystemName,
                version ?? "unknown",
                message);
        }
        else
        {
            logger.LogError(
                "Plugin lifecycle {Operation} for {PluginSystemName} v{Version} failed. {Message}",
                operation,
                pluginSystemName,
                version ?? "unknown",
                message);
        }
    }
}
