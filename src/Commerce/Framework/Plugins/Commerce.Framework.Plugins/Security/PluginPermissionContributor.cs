using Commerce.Framework.Contracts.Security;

namespace Commerce.Framework.Plugins.Security;

public static class PluginPermissions
{
    public const string View = "Plugins.View";
    public const string Manage = "Plugins.Manage";
    public const string Install = "Plugins.Install";
    public const string Configure = "Plugins.Configure";
}

public sealed class PluginPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Plugins";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(PluginPermissions.View, "View plugins.", ModuleSystemName),
        new(PluginPermissions.Manage, "Manage plugin lifecycle.", ModuleSystemName),
        new(PluginPermissions.Install, "Install plugins.", ModuleSystemName),
        new(PluginPermissions.Configure, "Configure plugins.", ModuleSystemName)
    ];
}
