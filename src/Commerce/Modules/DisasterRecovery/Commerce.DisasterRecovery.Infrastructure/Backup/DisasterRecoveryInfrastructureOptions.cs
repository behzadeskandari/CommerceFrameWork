namespace Commerce.DisasterRecovery.Infrastructure.Backup;

public sealed class DisasterRecoveryInfrastructureOptions
{
    public const string SectionName = "Commerce:DisasterRecovery";

    public string BackupRoot { get; set; } = "App_Data/backups";

    public int RetentionDays { get; set; } = 30;

    public int MinBackupsToKeep { get; set; } = 7;

    public int MaxBackupAgeHoursBeforeAlert { get; set; } = 26;

    public int MaxRestoreTestAgeDaysBeforeAlert { get; set; } = 30;

    public bool EnableScheduledBackups { get; set; } = true;

    public string MediaRoot { get; set; } = "App_Data/media";

    public string PluginsRoot { get; set; } = "Plugins";

    public bool MaskSecretsInConfigurationBackup { get; set; } = true;
}
