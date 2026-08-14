using Commerce.Framework.PluginContracts.Security;

namespace Commerce.Plugin.Test;

public sealed class TestPluginPermissionContributor : IPluginPermissionContributor
{
    public string PluginSystemName => "Commerce.Test";

    public IReadOnlyList<PluginPermissionDefinition> GetPermissions() =>
    [
        new("Commerce.Test.View", "View test plugin diagnostics.", PluginSystemName),
        new("Commerce.Test.Configure", "Configure test plugin settings.", PluginSystemName)
    ];
}
