using Commerce.Framework.Contracts.Security;

namespace Commerce.Customers.Infrastructure.Security;

public sealed class PermissionRegistry
{
    private readonly IReadOnlyList<PermissionDefinition> _permissions;

    public PermissionRegistry(IEnumerable<IModulePermissionContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        _permissions = contributors
            .SelectMany(contributor => contributor.GetPermissions())
            .DistinctBy(permission => permission.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission.ModuleSystemName)
            .ThenBy(permission => permission.Name, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<PermissionDefinition> GetAllPermissions() => _permissions;
}
