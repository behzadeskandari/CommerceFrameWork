using System.Data;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commerce.DisasterRecovery.Infrastructure.Backup;



public sealed class SqlServerDatabaseBackupProvider(
    CommerceDbContext dbContext,
    ILogger<SqlServerDatabaseBackupProvider> logger) : ISqlServerDatabaseBackupProvider
{
    public async Task<string> BackupDatabaseAsync(string targetFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFilePath);

        if (!dbContext.Database.IsSqlServer())
        {
            throw new NotSupportedException("Database backups are only supported for SQL Server.");
        }

        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Database name could not be determined from the connection string.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetFilePath))!);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "BACKUP DATABASE @database TO DISK = @path WITH INIT, CHECKSUM";
        command.CommandText = command.CommandText
            .Replace("@database", $"[{databaseName.Replace("]", "]]")}]");
        command.Parameters.Add(new SqlParameter("@path", Path.GetFullPath(targetFilePath)));
        command.CommandTimeout = 0;

        var record = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Database backup written to {Path}.", targetFilePath);
    
        return record > 0 ? Task.FromResult(targetFilePath).Result : Task.FromResult(record.ToString()).Result;
    }
}


//public sealed class SqlServerDatabaseBackupProvider : ISqlServerDatabaseBackupProvider
//{
//    private readonly IConfiguration _configuration;

//    public SqlServerDatabaseBackupProvider(IConfiguration configuration)
//    {
//        _configuration = configuration;
//    }

//    public async Task<string> BackupDatabaseAsync(string backupRoot, CancellationToken cancellationToken = default)
//    {
//        var dataOptions = _configuration.GetSection(CommerceDataOptions.SectionName).Get<CommerceDataOptions>() ?? new CommerceDataOptions();
//        var connectionString = dataOptions.ConnectionString;
//        if (string.IsNullOrWhiteSpace(connectionString))
//        {
//            throw new InvalidOperationException("Database connection string is not configured.");
//        }

//        var builder = new SqlConnectionStringBuilder(connectionString);
//        var database = builder.InitialCatalog;
//        if (string.IsNullOrWhiteSpace(database))
//        {
//            throw new InvalidOperationException("Database name could not be determined from the connection string.");
//        }

//        Directory.CreateDirectory(backupRoot);
//        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
//        var filename = Path.Combine(backupRoot, $"{database}-{timestamp}.bak");

//        await using var connection = new SqlConnection(connectionString);
//        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

//        var commandText = $"BACKUP DATABASE [{database}] TO DISK = N'{filename.Replace("'","''")}' WITH FORMAT";
//        await using var command = connection.CreateCommand();
//        command.CommandText = commandText;
//        command.CommandType = CommandType.Text;
//        command.CommandTimeout = 120;
//        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

//        return filename;
//    }
//}
