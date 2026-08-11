using Commerce.Framework.Contracts.Modules;

namespace Commerce.Modules.Core;

public sealed class CoreModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.core",
        SystemName: "Commerce.Core",
        Name: "Commerce Core",
        Version: new Version(1, 0, 0),
        Description: "Core Commerce platform module.",
        Dependencies: Array.Empty<ModuleDependency>(),
        IsRequired: true);
}
