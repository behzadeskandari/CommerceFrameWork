using Commerce.DisasterRecovery.Application.Mapping;
using Commerce.DisasterRecovery.Application.Services;
using Commerce.DisasterRecovery.Contracts;
using Commerce.DisasterRecovery.Domain.Entities;
using DomainEnums = Commerce.DisasterRecovery.Domain.Enums;

namespace Commerce.Tests.Unit.DisasterRecovery;

public sealed class BackupValidityTests
{
    [Fact]
    public void IsValidForRecovery_RequiresRestoreTestedStatus()
    {
        var run = BackupRun.Start("20260101-120000", "C:\\backups\\20260101-120000");
        run.Complete(DomainEnums.BackupRunStatus.Completed, "backup-manifest.json", "{}");

        Assert.False(run.IsValidForRecovery);

        run.MarkChecksumVerified();
        Assert.False(run.IsValidForRecovery);

        run.MarkRestoreTested();
        Assert.True(run.IsValidForRecovery);
    }

    [Fact]
    public void BackupMapper_ExposesValidityStatus()
    {
        var run = BackupRun.Start("20260101-120000", "C:\\backups\\20260101-120000");
        run.Complete(DomainEnums.BackupRunStatus.Completed, "backup-manifest.json", "{}");
        run.MarkRestoreTested();

        var dto = BackupMapper.ToDto(run);

        Assert.Equal(BackupValidityStatus.RestoreTested, dto.ValidityStatus);
        Assert.True(dto.IsValidForRecovery);
    }
}

public sealed class SecretMaskerTests
{
    [Fact]
    public void MaskConnectionStringSecrets_RedactsPassword()
    {
        const string json = """
            {
              "Provider": "SqlServer",
              "ConnectionString": "Server=localhost;Database=Commerce;User Id=sa;Password=123456;"
            }
            """;

        var masked = string.Empty;//Commerce.DisasterRecovery.Infrastructure.Backup.SecretMasker.MaskConnectionStringSecrets(json);

        Assert.DoesNotContain("Secret123!", masked, StringComparison.Ordinal);
        Assert.Contains("***", masked, StringComparison.Ordinal);
    }
}

public sealed class DisasterRecoveryMetadataTests
{
    [Fact]
    public void GetTargets_DefinesRpoAndRto()
    {
        var service = new DisasterRecoveryMetadataService();
        var targets = service.GetTargets();

        Assert.Equal(TimeSpan.FromHours(24), targets.RecoveryPointObjective);
        Assert.Equal(TimeSpan.FromHours(4), targets.RecoveryTimeObjective);
    }
}
