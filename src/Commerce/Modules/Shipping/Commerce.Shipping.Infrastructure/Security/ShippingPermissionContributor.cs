using Commerce.Framework.Contracts.Security;

namespace Commerce.Shipping.Infrastructure.Security;

public static class ShippingPermissions
{
    public const string View = "Shipping.View";
    public const string Manage = "Shipping.Manage";
    public const string Configure = "Shipping.Configure";
}

public sealed class ShippingPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Shipping";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(ShippingPermissions.View, "View shipping configuration.", ModuleSystemName),
        new(ShippingPermissions.Manage, "Manage shipping methods, zones, and rates.", ModuleSystemName),
        new(ShippingPermissions.Configure, "Configure shipping providers and settings.", ModuleSystemName)
    ];
}
