namespace Commerce.Framework.Contracts.Security;

public interface IModulePermissionContributor
{
    string ModuleSystemName { get; }

    IReadOnlyList<PermissionDefinition> GetPermissions();
}
