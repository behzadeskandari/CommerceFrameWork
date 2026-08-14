using Commerce.Framework.Contracts.Security;

namespace Commerce.DisasterRecovery.Infrastructure.Security;

public static class DisasterRecoveryPermissions
{
    public const string View = "DisasterRecovery.View";
    public const string CreateBackup = "DisasterRecovery.CreateBackup";
    public const string VerifyBackup = "DisasterRecovery.VerifyBackup";
    public const string RunRecoveryTest = "DisasterRecovery.RunRecoveryTest";
    public const string ManageRetention = "DisasterRecovery.ManageRetention";
}

public sealed class DisasterRecoveryPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.DisasterRecovery";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(DisasterRecoveryPermissions.View, "View backup status and integrity reports.", ModuleSystemName),
        new(DisasterRecoveryPermissions.CreateBackup, "Create disaster recovery backups.", ModuleSystemName),
        new(DisasterRecoveryPermissions.VerifyBackup, "Verify backup checksums.", ModuleSystemName),
        new(DisasterRecoveryPermissions.RunRecoveryTest, "Run backup recovery tests.", ModuleSystemName),
        new(DisasterRecoveryPermissions.ManageRetention, "Apply backup retention policy.", ModuleSystemName)
    ];
}
