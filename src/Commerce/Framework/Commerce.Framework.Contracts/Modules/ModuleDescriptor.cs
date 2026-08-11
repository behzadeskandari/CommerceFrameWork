namespace Commerce.Framework.Contracts.Modules;

public sealed record ModuleDescriptor(
    string Id,
    string SystemName,
    string Name,
    Version Version,
    string Description,
    IReadOnlyList<ModuleDependency> Dependencies,
    bool IsRequired = true);
