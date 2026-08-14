using Commerce.Framework.Contracts.Security;

namespace Commerce.Cms.Infrastructure.Security;

public static class CmsPermissions
{
    public const string PagesView = "Cms.Pages.View";
    public const string PagesManage = "Cms.Pages.Manage";
    public const string TopicsView = "Cms.Topics.View";
    public const string TopicsManage = "Cms.Topics.Manage";
    public const string MenusView = "Cms.Menus.View";
    public const string MenusManage = "Cms.Menus.Manage";
    public const string WidgetsView = "Cms.Widgets.View";
    public const string WidgetsManage = "Cms.Widgets.Manage";
}

public sealed class CmsPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Cms";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(CmsPermissions.PagesView, "View CMS pages.", ModuleSystemName),
        new(CmsPermissions.PagesManage, "Manage CMS pages.", ModuleSystemName),
        new(CmsPermissions.TopicsView, "View CMS topics.", ModuleSystemName),
        new(CmsPermissions.TopicsManage, "Manage CMS topics.", ModuleSystemName),
        new(CmsPermissions.MenusView, "View CMS menus.", ModuleSystemName),
        new(CmsPermissions.MenusManage, "Manage CMS menus.", ModuleSystemName),
        new(CmsPermissions.WidgetsView, "View CMS widgets.", ModuleSystemName),
        new(CmsPermissions.WidgetsManage, "Manage CMS widgets.", ModuleSystemName)
    ];
}
