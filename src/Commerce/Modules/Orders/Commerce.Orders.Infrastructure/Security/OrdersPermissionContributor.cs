using Commerce.Framework.Contracts.Security;

namespace Commerce.Orders.Infrastructure.Security;

public static class OrdersPermissions
{
    public const string View = "Orders.View";
    public const string Manage = "Orders.Manage";
    public const string Cancel = "Orders.Cancel";
}

public sealed class OrdersPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Orders";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(OrdersPermissions.View, "View orders.", ModuleSystemName),
        new(OrdersPermissions.Manage, "Manage orders.", ModuleSystemName),
        new(OrdersPermissions.Cancel, "Cancel orders.", ModuleSystemName)
    ];
}
