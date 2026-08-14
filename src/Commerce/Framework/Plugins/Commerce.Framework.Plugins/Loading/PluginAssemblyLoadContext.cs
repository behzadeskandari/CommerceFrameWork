using System.Reflection;
using System.Runtime.Loader;

namespace Commerce.Framework.Plugins.Loading;

internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDirectory;

    public PluginAssemblyLoadContext(string pluginDirectory) : base(isCollectible: true)
    {
        _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var defaultAssembly = AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (defaultAssembly is not null)
        {
            return defaultAssembly;
        }

        var dependencyPath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(dependencyPath))
        {
            return LoadFromAssemblyPath(dependencyPath);
        }

        var dependenciesDirectory = Path.Combine(_pluginDirectory, "dependencies");
        var nestedDependencyPath = Path.Combine(dependenciesDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(nestedDependencyPath))
        {
            return LoadFromAssemblyPath(nestedDependencyPath);
        }

        return null;
    }
}
