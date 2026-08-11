using Commerce.Framework.Contracts.Modules;

namespace Commerce.Framework.Application.Modules;

public sealed class ModuleRegistrationContext
{
    private readonly Dictionary<string, ModuleEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public ModuleRegistrationContext(
        IReadOnlyList<ICommerceModule> modules,
        IReadOnlyList<ModuleDescriptor> orderedDescriptors,
        IReadOnlySet<string> disabledSystemNames)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(orderedDescriptors);
        ArgumentNullException.ThrowIfNull(disabledSystemNames);

        Modules = modules;
        OrderedDescriptors = orderedDescriptors;
        DisabledSystemNames = disabledSystemNames;

        foreach (var module in modules)
        {
            _entries[module.Descriptor.SystemName] = new ModuleEntry(module, ModuleState.Discovered);
        }
    }

    public IReadOnlyList<ICommerceModule> Modules { get; }

    public IReadOnlyList<ModuleDescriptor> OrderedDescriptors { get; }

    public IReadOnlySet<string> DisabledSystemNames { get; }

    public IReadOnlyList<string> OrderedSystemNames =>
        OrderedDescriptors.Select(x => x.SystemName).ToList();

    internal ModuleEntry GetEntry(string systemName) => _entries[systemName];

    internal IReadOnlyDictionary<string, ModuleEntry> Entries => _entries;

    internal sealed class ModuleEntry(ICommerceModule module, ModuleState state)
    {
        public ICommerceModule Module { get; } = module;

        public ModuleState State { get; set; } = state;

        public TimeSpan? StartupDuration { get; set; }

        public string? FailureReason { get; set; }
    }
}
