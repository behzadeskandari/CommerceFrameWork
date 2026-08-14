using Commerce.Framework.Contracts.Security;
using Commerce.Framework.PluginContracts.Security;

namespace Commerce.Framework.Plugins.Security;

public sealed class PluginDynamicPermissionContributor(IEnumerable<IPluginPermissionContributor> contributors)
    : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Plugins.Dynamic";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
        contributors
            .SelectMany(contributor => contributor.GetPermissions().Select(permission =>
                new PermissionDefinition(permission.Key, permission.Description, permission.PluginSystemName)))
            .ToList();
}

public sealed class PluginPermissionRegistry
{
    private readonly Dictionary<string, IReadOnlyList<PluginPermissionDefinition>> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public PluginPermissionRegistry(IEnumerable<IPluginPermissionContributor> contributors)
    {
        foreach (var contributor in contributors)
        {
            _permissions[contributor.PluginSystemName] = contributor.GetPermissions();
        }
    }

    public IReadOnlyList<PluginPermissionDefinition> GetPermissions(string pluginSystemName) =>
        _permissions.TryGetValue(pluginSystemName, out var permissions)
            ? permissions
            : Array.Empty<PluginPermissionDefinition>();
}
