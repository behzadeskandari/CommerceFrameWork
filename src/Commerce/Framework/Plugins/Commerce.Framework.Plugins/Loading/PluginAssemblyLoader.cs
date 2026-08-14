using Commerce.Framework.PluginContracts.Loading;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Plugins.Loading;

public sealed class PluginAssemblyLoader(ILogger<PluginAssemblyLoader> logger) : IPluginAssemblyLoader
{
    private readonly Dictionary<string, PluginAssemblyLoadContext> _contexts = new(StringComparer.OrdinalIgnoreCase);

    public LoadedPluginAssembly Load(PluginDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        Unload(descriptor.SystemName);

        var assemblyPath = Path.Combine(descriptor.PluginDirectory, descriptor.AssemblyName);
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Plugin assembly not found at '{assemblyPath}'.", assemblyPath);
        }

        var loadContext = new PluginAssemblyLoadContext(descriptor.PluginDirectory);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        var pluginType = assembly.ExportedTypes
            .FirstOrDefault(t => typeof(ICommercePlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        if (pluginType is null)
        {
            loadContext.Unload();
            throw new InvalidOperationException(
                $"Plugin assembly '{descriptor.AssemblyName}' does not contain an ICommercePlugin implementation.");
        }

        var plugin = (ICommercePlugin)Activator.CreateInstance(pluginType)!;
        _contexts[descriptor.SystemName] = loadContext;

        logger.LogInformation("Loaded plugin assembly {PluginSystemName} from {AssemblyPath}.", descriptor.SystemName, assemblyPath);

        return new LoadedPluginAssembly(descriptor, plugin, assembly, loadContext);
    }

    public void Unload(string systemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);

        if (_contexts.Remove(systemName, out var context))
        {
            context.Unload();
        }
    }
}
