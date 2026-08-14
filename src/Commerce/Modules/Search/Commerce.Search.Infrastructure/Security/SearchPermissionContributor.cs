using Commerce.Framework.Contracts.Security;

namespace Commerce.Search.Infrastructure.Security;

public static class SearchPermissions
{
    public const string View = "Search.View";
    public const string Manage = "Search.Manage";
}

public sealed class SearchPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Search";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(SearchPermissions.View, "View search index status.", ModuleSystemName),
        new(SearchPermissions.Manage, "Manage search indexing.", ModuleSystemName)
    ];
}
