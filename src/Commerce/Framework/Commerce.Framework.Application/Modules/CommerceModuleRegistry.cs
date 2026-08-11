using Commerce.Framework.Contracts.Modules;

namespace Commerce.Framework.Application.Modules;

public sealed class CommerceModuleRegistry(ModuleRegistrationContext context) : ICommerceModuleRegistry
{
    public IReadOnlyList<ModuleRuntimeInfo> GetModules() =>
        context.OrderedDescriptors
            .Select(descriptor => ToRuntimeInfo(descriptor, context.GetEntry(descriptor.SystemName)))
            .ToList();

    public ModuleRuntimeInfo? GetModule(string systemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);

        var entry = context.Entries.Values
            .FirstOrDefault(x => string.Equals(x.Module.Descriptor.SystemName, systemName, StringComparison.OrdinalIgnoreCase));

        return entry is null ? null : ToRuntimeInfo(entry.Module.Descriptor, entry);
    }

    public IReadOnlyList<ModuleRuntimeInfo> GetModulesInDependencyOrder() => GetModules();

    private static ModuleRuntimeInfo ToRuntimeInfo(
        ModuleDescriptor descriptor,
        ModuleRegistrationContext.ModuleEntry entry) =>
        new(descriptor, entry.State, entry.StartupDuration, entry.FailureReason);
}
