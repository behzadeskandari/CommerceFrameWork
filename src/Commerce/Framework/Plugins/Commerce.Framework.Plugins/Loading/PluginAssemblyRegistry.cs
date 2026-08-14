using System.Reflection;

namespace Commerce.Framework.Plugins.Loading;

public sealed class PluginAssemblyRegistry
{
    private readonly Dictionary<string, Assembly> _assembliesBySystemName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Assembly, string> _systemNamesByAssembly = new();

    public static PluginAssemblyRegistry Instance { get; } = new();

    public IReadOnlyDictionary<string, Assembly> Assemblies => _assembliesBySystemName;

    public void Register(string systemName, Assembly assembly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);
        ArgumentNullException.ThrowIfNull(assembly);

        _assembliesBySystemName[systemName] = assembly;
        _systemNamesByAssembly[assembly] = systemName;
    }

    public void Unregister(string systemName)
    {
        if (_assembliesBySystemName.Remove(systemName, out var assembly))
        {
            _systemNamesByAssembly.Remove(assembly);
        }
    }

    public bool TryGetSystemName(Assembly assembly, out string systemName) =>
        _systemNamesByAssembly.TryGetValue(assembly, out systemName!);

    public void Clear()
    {
        _assembliesBySystemName.Clear();
        _systemNamesByAssembly.Clear();
    }
}
