using Commerce.Framework.Contracts.Security;

namespace Commerce.Pricing.Infrastructure.Security;

public static class PricingPermissions
{
    public const string DiscountsView = "Discounts.View";
    public const string DiscountsManage = "Discounts.Manage";
    public const string DiscountsCreate = "Discounts.Create";
    public const string DiscountsUpdate = "Discounts.Update";
    public const string DiscountsDelete = "Discounts.Delete";
    public const string CouponsView = "Coupons.View";
    public const string CouponsManage = "Coupons.Manage";
    public const string CustomerGroupsView = "CustomerGroups.View";
    public const string CustomerGroupsManage = "CustomerGroups.Manage";
}

public sealed class PricingPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Pricing";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(PricingPermissions.DiscountsView, "View discounts.", ModuleSystemName),
        new(PricingPermissions.DiscountsManage, "Manage discounts.", ModuleSystemName),
        new(PricingPermissions.DiscountsCreate, "Create discounts.", ModuleSystemName),
        new(PricingPermissions.DiscountsUpdate, "Update discounts.", ModuleSystemName),
        new(PricingPermissions.DiscountsDelete, "Delete discounts.", ModuleSystemName),
        new(PricingPermissions.CouponsView, "View coupons.", ModuleSystemName),
        new(PricingPermissions.CouponsManage, "Manage coupons.", ModuleSystemName),
        new(PricingPermissions.CustomerGroupsView, "View customer groups.", ModuleSystemName),
        new(PricingPermissions.CustomerGroupsManage, "Manage customer groups.", ModuleSystemName)
    ];
}
