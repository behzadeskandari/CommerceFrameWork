using Commerce.Framework.Contracts.Security;

namespace Commerce.Reviews.Infrastructure.Security;

public static class ReviewPermissions
{
    public const string View = "Reviews.View";
    public const string Manage = "Reviews.Manage";
}

public sealed class ReviewPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Reviews";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(ReviewPermissions.View, "View product reviews, ratings, and wishlists.", ModuleSystemName),
        new(ReviewPermissions.Manage, "Moderate reviews and manage wishlist data.", ModuleSystemName)
    ];
}
