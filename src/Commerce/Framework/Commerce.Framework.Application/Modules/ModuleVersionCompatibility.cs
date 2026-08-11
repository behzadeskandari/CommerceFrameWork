using Commerce.Framework.Contracts.Modules;

namespace Commerce.Framework.Application.Modules;

public static class ModuleVersionCompatibility
{
    public static void ValidateDependency(ModuleDescriptor module, ModuleDependency dependency, ModuleDescriptor dependencyModule)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(dependencyModule);

        if (!string.Equals(dependency.ModuleSystemName, dependencyModule.SystemName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ModuleDependencyResolutionException(
                $"Internal dependency validation mismatch for module '{module.SystemName}'.");
        }

        if (string.IsNullOrWhiteSpace(dependency.MinimumVersion))
        {
            return;
        }

        if (!Version.TryParse(dependency.MinimumVersion, out var minimumVersion))
        {
            throw new ModuleDependencyResolutionException(
                $"Module '{module.SystemName}' declares invalid minimum version '{dependency.MinimumVersion}' for dependency '{dependency.ModuleSystemName}'.");
        }

        if (dependencyModule.Version < minimumVersion)
        {
            throw new ModuleDependencyResolutionException(
                $"Module '{module.SystemName}' requires '{dependency.ModuleSystemName}' version {minimumVersion} or later, but version {dependencyModule.Version} is available.");
        }
    }
}
