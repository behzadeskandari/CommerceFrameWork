using Commerce.Framework.Contracts.Security;

namespace Commerce.Promotions.Infrastructure.Security;

public static class PromotionPermissions
{
    public const string View = "Promotions.View";
    public const string Manage = "Promotions.Manage";
}

public sealed class PromotionPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Promotions";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(PromotionPermissions.View, "View promotions.", ModuleSystemName),
        new(PromotionPermissions.Manage, "Manage promotions and rules.", ModuleSystemName)
    ];
}
