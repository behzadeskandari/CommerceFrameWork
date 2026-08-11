using Commerce.Framework.Contracts.Security;

namespace Commerce.Inventory.Infrastructure.Security;

public static class InventoryPermissions
{
    public const string View = "Inventory.View";
    public const string Manage = "Inventory.Manage";
    public const string Adjust = "Inventory.Adjust";
    public const string Reserve = "Inventory.Reserve";
}

public sealed class InventoryPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Inventory";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(InventoryPermissions.View, "View inventory.", ModuleSystemName),
        new(InventoryPermissions.Manage, "Manage inventory items.", ModuleSystemName),
        new(InventoryPermissions.Adjust, "Adjust inventory stock.", ModuleSystemName),
        new(InventoryPermissions.Reserve, "Manage inventory reservations.", ModuleSystemName)
    ];
}
