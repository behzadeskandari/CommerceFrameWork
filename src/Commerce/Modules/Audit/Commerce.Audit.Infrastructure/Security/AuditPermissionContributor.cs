using Commerce.Framework.Contracts.Security;

namespace Commerce.Audit.Infrastructure.Security;

public static class AuditPermissions
{
    public const string View = "Audit.View";
    public const string Export = "Audit.Export";
    public const string ManageRetention = "Audit.ManageRetention";
    public const string VerifyChain = "Audit.VerifyChain";
}

public sealed class AuditPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Audit";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(AuditPermissions.View, "View audit log entries.", ModuleSystemName),
        new(AuditPermissions.Export, "Export audit log entries.", ModuleSystemName),
        new(AuditPermissions.VerifyChain, "Verify audit hash chain integrity.", ModuleSystemName),
        new(AuditPermissions.ManageRetention, "Apply audit retention policy.", ModuleSystemName)
    ];
}
