namespace Commerce.Framework.PluginContracts.Security;

public sealed record PluginPermissionDefinition(
    string Key,
    string Description,
    string PluginSystemName);

public interface IPluginPermissionContributor
{
    string PluginSystemName { get; }

    IReadOnlyList<PluginPermissionDefinition> GetPermissions();
}
