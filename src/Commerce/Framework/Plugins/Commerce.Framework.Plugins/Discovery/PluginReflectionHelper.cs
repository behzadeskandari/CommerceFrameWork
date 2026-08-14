using System.Reflection;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.Plugins.Loading;

namespace Commerce.Framework.Plugins.Discovery;

public static class PluginReflectionHelper
{
    public static Assembly? TryLoadReadOnlyAssembly(PluginDescriptor descriptor)
    {
        if (PluginAssemblyRegistry.Instance.Assemblies.TryGetValue(descriptor.SystemName, out var registered))
        {
            return registered;
        }

        var assemblyPath = Path.Combine(descriptor.PluginDirectory, descriptor.AssemblyName);
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        return Assembly.LoadFrom(assemblyPath);
    }
}
