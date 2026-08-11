using Commerce.Framework.Contracts.Modules;

namespace Commerce.Framework.Application.Modules;

public static class ModuleDependencyResolver
{
    public static IReadOnlyList<ModuleDescriptor> Resolve(
        IReadOnlyList<ModuleDescriptor> modules,
        IReadOnlySet<string>? disabledSystemNames = null)
    {
        ArgumentNullException.ThrowIfNull(modules);

        disabledSystemNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ValidateMetadata(modules);

        var activeModules = modules
            .Where(m => !disabledSystemNames.Contains(m.SystemName))
            .ToList();

        var lookup = activeModules.ToDictionary(m => m.SystemName, StringComparer.OrdinalIgnoreCase);

        foreach (var module in activeModules)
        {
            foreach (var dependency in module.Dependencies)
            {
                if (!lookup.TryGetValue(dependency.ModuleSystemName, out var dependencyModule))
                {
                    throw new ModuleDependencyResolutionException(
                        $"Module {module.SystemName} requires {dependency.ModuleSystemName}, but {dependency.ModuleSystemName} is not installed.");
                }

                ModuleVersionCompatibility.ValidateDependency(module, dependency, dependencyModule);
            }
        }

        return TopologicalSort(activeModules);
    }

    private static void ValidateMetadata(IReadOnlyList<ModuleDescriptor> modules)
    {
        var duplicateIds = modules
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateIds is not null)
        {
            throw new ModuleDependencyResolutionException(
                $"Duplicate module Id '{duplicateIds.Key}' detected.");
        }

        var duplicateSystemNames = modules
            .GroupBy(m => m.SystemName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateSystemNames is not null)
        {
            throw new ModuleDependencyResolutionException(
                $"Duplicate module SystemName '{duplicateSystemNames.Key}' detected.");
        }

        foreach (var module in modules)
        {
            if (string.IsNullOrWhiteSpace(module.Id))
            {
                throw new ModuleDependencyResolutionException("Module Id is required.");
            }

            if (string.IsNullOrWhiteSpace(module.SystemName))
            {
                throw new ModuleDependencyResolutionException("Module SystemName is required.");
            }

            if (module.Version is null)
            {
                throw new ModuleDependencyResolutionException(
                    $"Module '{module.SystemName}' has an invalid version.");
            }
        }
    }

    private static IReadOnlyList<ModuleDescriptor> TopologicalSort(IReadOnlyList<ModuleDescriptor> modules)
    {
        var lookup = modules.ToDictionary(m => m.SystemName, StringComparer.OrdinalIgnoreCase);
        var inDegree = modules.ToDictionary(m => m.SystemName, _ => 0, StringComparer.OrdinalIgnoreCase);
        var dependents = modules.ToDictionary(m => m.SystemName, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var module in modules)
        {
            foreach (var dependency in module.Dependencies)
            {
                if (!lookup.ContainsKey(dependency.ModuleSystemName))
                {
                    continue;
                }

                inDegree[module.SystemName]++;
                dependents[dependency.ModuleSystemName].Add(module.SystemName);
            }
        }

        var queue = new Queue<string>(inDegree.Where(x => x.Value == 0).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal));
        var ordered = new List<ModuleDescriptor>();

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

        if (ordered.Count != modules.Count)
        {
            throw new ModuleDependencyResolutionException("Circular module dependency detected.");
        }

        return ordered;
    }
}
