using Commerce.Framework.Contracts.Security;

namespace Commerce.Customers.Infrastructure.Security;

public static class CustomersPermissions
{
    public const string View = "Customers.View";

    public const string Update = "Customers.Update";

    public const string Manage = "Customers.Manage";

    public const string LoyaltyView = "Customers.Loyalty.View";

    public const string LoyaltyManage = "Customers.Loyalty.Manage";

    public const string SegmentsView = "Customers.Segments.View";

    public const string SegmentsManage = "Customers.Segments.Manage";

    public const string StoreCreditManage = "Customers.StoreCredit.Manage";

    public const string AffiliatesView = "Customers.Affiliates.View";

    public const string AffiliatesManage = "Customers.Affiliates.Manage";
}

public sealed class CustomersPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Customers";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(CustomersPermissions.View, "View customers.", ModuleSystemName),
        new(CustomersPermissions.Update, "Update customers.", ModuleSystemName),
        new(CustomersPermissions.Manage, "Manage customer accounts.", ModuleSystemName),
        new(CustomersPermissions.LoyaltyView, "View loyalty accounts.", ModuleSystemName),
        new(CustomersPermissions.LoyaltyManage, "Manage loyalty and rewards.", ModuleSystemName),
        new(CustomersPermissions.SegmentsView, "View customer segments.", ModuleSystemName),
        new(CustomersPermissions.SegmentsManage, "Manage customer segments.", ModuleSystemName),
        new(CustomersPermissions.StoreCreditManage, "Manage customer store credit.", ModuleSystemName),
        new(CustomersPermissions.AffiliatesView, "View affiliates and referrals.", ModuleSystemName),
        new(CustomersPermissions.AffiliatesManage, "Manage affiliates and commissions.", ModuleSystemName)
    ];
}
