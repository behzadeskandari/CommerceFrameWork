using Commerce.Framework.Contracts.Security;

namespace Commerce.Scheduling.Infrastructure.Security;

public static class SchedulingPermissions
{
    public const string View = "Scheduling.View";
    public const string Manage = "Scheduling.Manage";
}

public sealed class SchedulingPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Scheduling";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(SchedulingPermissions.View, "View background jobs and recurring schedules.", ModuleSystemName),
        new(SchedulingPermissions.Manage, "Cancel, retry, and manage background jobs.", ModuleSystemName)
    ];
}
