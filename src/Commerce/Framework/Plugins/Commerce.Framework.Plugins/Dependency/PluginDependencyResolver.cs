using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.Plugins.Dependency;

public sealed class PluginDependencyResolutionException : Exception
{
    public PluginDependencyResolutionException(string message) : base(message)
    {
    }
}

public static class PluginDependencyResolver
{
    public static IReadOnlyList<PluginDescriptor> Resolve(
        IReadOnlyList<PluginDescriptor> plugins,
        IReadOnlySet<string>? disabledSystemNames = null)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        disabledSystemNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ValidateMetadata(plugins);

        var activePlugins = plugins
            .Where(p => !disabledSystemNames.Contains(p.SystemName))
            .ToList();

        var lookup = activePlugins.ToDictionary(p => p.SystemName, StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in activePlugins)
        {
            foreach (var dependency in plugin.Dependencies)
            {
                if (!lookup.TryGetValue(dependency.PluginSystemName, out _))
                {
                    throw new PluginDependencyResolutionException(
                        $"Plugin {plugin.SystemName} requires {dependency.PluginSystemName}, but {dependency.PluginSystemName} was not discovered.");
                }
            }
        }

        return TopologicalSort(activePlugins);
    }

    private static void ValidateMetadata(IReadOnlyList<PluginDescriptor> plugins)
    {
        var duplicateSystemNames = plugins
            .GroupBy(p => p.SystemName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateSystemNames is not null)
        {
            throw new PluginDependencyResolutionException(
                $"Duplicate plugin SystemName '{duplicateSystemNames.Key}' detected.");
        }

        foreach (var plugin in plugins)
        {
            if (string.IsNullOrWhiteSpace(plugin.SystemName))
            {
                throw new PluginDependencyResolutionException("Plugin SystemName is required.");
            }

            if (plugin.Version is null)
            {
                throw new PluginDependencyResolutionException(
                    $"Plugin '{plugin.SystemName}' has an invalid version.");
            }
        }
    }

    private static IReadOnlyList<PluginDescriptor> TopologicalSort(IReadOnlyList<PluginDescriptor> plugins)
    {
        var lookup = plugins.ToDictionary(p => p.SystemName, StringComparer.OrdinalIgnoreCase);
        var inDegree = plugins.ToDictionary(p => p.SystemName, _ => 0, StringComparer.OrdinalIgnoreCase);
        var dependents = plugins.ToDictionary(p => p.SystemName, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in plugins)
        {
            foreach (var dependency in plugin.Dependencies)
            {
                if (!lookup.ContainsKey(dependency.PluginSystemName))
                {
                    continue;
                }

                inDegree[plugin.SystemName]++;
                dependents[dependency.PluginSystemName].Add(plugin.SystemName);
            }
        }

        var queue = new Queue<string>(inDegree.Where(x => x.Value == 0).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal));
        var ordered = new List<PluginDescriptor>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            ordered.Add(lookup[current]);

            foreach (var dependent in dependents[current].OrderBy(x => x, StringComparer.Ordinal))
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        if (ordered.Count != plugins.Count)
        {
            throw new PluginDependencyResolutionException("Circular plugin dependency detected.");
        }

        return ordered;
    }
}
