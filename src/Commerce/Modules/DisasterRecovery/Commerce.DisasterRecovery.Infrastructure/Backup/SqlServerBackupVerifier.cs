using System.Data;
using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commerce.DisasterRecovery.Infrastructure.Backup;

public sealed class SqlServerBackupVerifier(
    CommerceDbContext dbContext,
    ILogger<SqlServerBackupVerifier> logger) : ISqlServerBackupVerifier
{
    public async Task<bool> VerifyOnlyAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            return false;
        }

        if (!dbContext.Database.IsSqlServer())
        {
            return false;
        }

        var connectionString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "RESTORE VERIFYONLY FROM DISK = @path";
            command.Parameters.Add(new SqlParameter("@path", Path.GetFullPath(backupFilePath)));
            command.CommandTimeout = 0;

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RESTORE VERIFYONLY failed for backup {Path}.", backupFilePath);
            return false;
        }
    }
}
//public sealed class SqlServerBackupVerifier : Commerce.DisasterRecovery.Application.Abstractions.ISqlServerBackupVerifier
//{
//    private readonly IConfiguration _configuration;

//    public SqlServerBackupVerifier(IConfiguration configuration)
//    {
//        _configuration = configuration;
//    }

//    public async Task<bool> VerifyOnlyAsync(string backupFilePath, CancellationToken cancellationToken = default)
//    {
//        if (!File.Exists(backupFilePath)) return false;

//        var dataOptions = _configuration.GetSection(CommerceDataOptions.SectionName).Get<CommerceDataOptions>() ?? new CommerceDataOptions();
//        var connectionString = dataOptions.ConnectionString;
//        if (string.IsNullOrWhiteSpace(connectionString))
//        {
//            throw new InvalidOperationException("Database connection string is not configured.");
//        }

//        await using var connection = new SqlConnection(connectionString);
//        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

//        var cmd = connection.CreateCommand();
//        cmd.CommandText = $"RESTORE VERIFYONLY FROM DISK = N'{backupFilePath.Replace("'","''")}'";
//        cmd.CommandType = CommandType.Text;
//        try
//        {
//            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
//            return true;
//        }
//        catch
//        {
//            return false;
//        }
//    }
//}
