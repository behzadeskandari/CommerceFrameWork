using Commerce.Framework.Contracts.Security;

namespace Commerce.Downloads.Infrastructure.Security;

public static class DownloadPermissions
{
    public const string View = "Downloads.View";
    public const string Manage = "Downloads.Manage";
    public const string Configure = "Downloads.Configure";
}

public sealed class DownloadPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Downloads";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(DownloadPermissions.View, "View download configuration and history.", ModuleSystemName),
        new(DownloadPermissions.Manage, "Manage customer download entitlements.", ModuleSystemName),
        new(DownloadPermissions.Configure, "Configure product download files and limits.", ModuleSystemName)
    ];
}
