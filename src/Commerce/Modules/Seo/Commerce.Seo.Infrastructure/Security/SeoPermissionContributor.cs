using Commerce.Framework.Contracts.Security;

namespace Commerce.Seo.Infrastructure.Security;

public static class SeoPermissions
{
    public const string View = "Seo.View";
    public const string Manage = "Seo.Manage";
}

public sealed class SeoPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Seo";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(SeoPermissions.View, "View SEO settings and URL records.", ModuleSystemName),
        new(SeoPermissions.Manage, "Manage SEO metadata, URLs, and robots.", ModuleSystemName)
    ];
}
