using Commerce.Framework.Contracts.Security;

namespace Commerce.Notifications.Infrastructure.Security;

public static class NotificationPermissions
{
    public const string View = "Notifications.View";
    public const string Manage = "Notifications.Manage";
}

public sealed class NotificationPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Notifications";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(NotificationPermissions.View, "View notification templates and delivery history.", ModuleSystemName),
        new(NotificationPermissions.Manage, "Manage notification templates and retry deliveries.", ModuleSystemName)
    ];
}
