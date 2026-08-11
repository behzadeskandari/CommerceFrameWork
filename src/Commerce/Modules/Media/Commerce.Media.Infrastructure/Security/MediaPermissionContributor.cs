using Commerce.Framework.Contracts.Security;

namespace Commerce.Media.Infrastructure.Security;

public static class MediaPermissions
{
    public const string View = "Media.View";
    public const string Upload = "Media.Upload";
    public const string Update = "Media.Update";
    public const string Delete = "Media.Delete";
}

public sealed class MediaPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Media";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(MediaPermissions.View, "View media library.", ModuleSystemName),
        new(MediaPermissions.Upload, "Upload media files.", ModuleSystemName),
        new(MediaPermissions.Update, "Update media metadata.", ModuleSystemName),
        new(MediaPermissions.Delete, "Delete media files.", ModuleSystemName)
    ];
}
