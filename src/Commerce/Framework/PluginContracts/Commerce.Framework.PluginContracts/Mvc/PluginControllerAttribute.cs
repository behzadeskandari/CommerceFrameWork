namespace Commerce.Framework.PluginContracts.Mvc;

/// <summary>
/// Marks a controller as belonging to a Commerce plugin.
/// Routes are prefixed with <c>/api/plugins/{pluginSystemName}/</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PluginControllerAttribute(string pluginSystemName) : Attribute
{
    public string PluginSystemName { get; } = pluginSystemName;
}
