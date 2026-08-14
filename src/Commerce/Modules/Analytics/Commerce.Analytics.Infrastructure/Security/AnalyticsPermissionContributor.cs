using Commerce.Framework.Contracts.Security;

namespace Commerce.Analytics.Infrastructure.Security;

public static class AnalyticsPermissions
{
    public const string View = "Analytics.View";
    public const string ReportsView = "Analytics.Reports.View";
    public const string ReportsExport = "Analytics.Reports.Export";
}

public sealed class AnalyticsPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Analytics";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(AnalyticsPermissions.View, "View analytics dashboard.", ModuleSystemName),
        new(AnalyticsPermissions.ReportsView, "View analytics reports.", ModuleSystemName),
        new(AnalyticsPermissions.ReportsExport, "Export analytics reports.", ModuleSystemName)
    ];
}
