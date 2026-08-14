using Commerce.Framework.Contracts.Security;

namespace Commerce.Themes.Infrastructure.Security;

public static class ThemePermissions
{
    public const string View = "Themes.View";
    public const string Manage = "Themes.Manage";
}

public sealed class ThemePermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Themes";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(ThemePermissions.View, "View themes.", ModuleSystemName),
        new(ThemePermissions.Manage, "Manage themes.", ModuleSystemName)
    ];
}
