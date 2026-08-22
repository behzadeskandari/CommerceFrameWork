namespace Commerce.DisasterRecovery.Infrastructure.Backup;

public interface ISqlServerDatabaseBackupProvider
{
    /// <summary>
    /// Creates a database backup under <paramref name="backupRoot"/> and returns the full path to the backup file.
    /// </summary>
    Task<string> BackupDatabaseAsync(string targetFilePath, CancellationToken cancellationToken = default);

}
