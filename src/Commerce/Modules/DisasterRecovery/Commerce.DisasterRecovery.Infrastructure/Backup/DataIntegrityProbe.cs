using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.Framework.Data.Db;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.DisasterRecovery.Infrastructure.Backup;


public sealed class DataIntegrityProbe(
    CommerceDbContext dbContext,
    IOptions<DisasterRecoveryInfrastructureOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<DataIntegrityProbe> logger) : IDataIntegrityProbe
{
    public async Task<LiveIntegrityProbeSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
    {
        var settingsCount = await dbContext.Settings
            .AsNoTracking()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var migrationVersions = await dbContext.MigrationVersionInfo
            .AsNoTracking()
            .Select(x => x.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var mediaAssetCount = await CountTableAsync("MediaAssets", cancellationToken).ConfigureAwait(false);
        var downloadEntitlementCount = await CountTableAsync("DownloadEntitlements", cancellationToken).ConfigureAwait(false);
        var installedPlugins = await ListInstalledPluginsAsync(cancellationToken).ConfigureAwait(false);

        var mediaRoot = ResolvePath(options.Value.MediaRoot);
        var mediaFileCount = Directory.Exists(mediaRoot)
            ? Directory.EnumerateFiles(mediaRoot, "*", SearchOption.AllDirectories).Count()
            : 0;

        var pluginsRoot = ResolvePath(options.Value.PluginsRoot);
        var pluginFolderCount = Directory.Exists(pluginsRoot)
            ? Directory.EnumerateFiles(pluginsRoot, "Plugin.json", SearchOption.AllDirectories).Count()
            : 0;

        return new LiveIntegrityProbeSnapshot(
            mediaAssetCount,
            mediaFileCount,
            downloadEntitlementCount,
            installedPlugins.Count,
            pluginFolderCount,
            settingsCount,
            migrationVersions,
            installedPlugins);
    }

    private string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(hostEnvironment.ContentRootPath, path);

    private async Task<int> CountTableAsync(string tableName, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return 0;
        }

        try
        {
            await using var connection = new SqlConnection(dbContext.Database.GetConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"IF OBJECT_ID(N'[{tableName}]', N'U') IS NOT NULL SELECT COUNT(*) FROM [{tableName}] ELSE SELECT 0";
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt32(result ?? 0);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Integrity probe could not count table {Table}.", tableName);
            return 0;
        }
    }

    private async Task<IReadOnlyList<string>> ListInstalledPluginsAsync(CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return [];
        }

        try
        {
            await using var connection = new SqlConnection(dbContext.Database.GetConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText =
                "IF OBJECT_ID(N'[CommercePluginInstallations]', N'U') IS NOT NULL " +
                "SELECT [SystemName] FROM [CommercePluginInstallations] WHERE [IsInstalled] = 1";
            var plugins = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                plugins.Add(reader.GetString(0));
            }

            return plugins;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Integrity probe could not list installed plugins.");
            return [];
        }
    }
}

//public sealed class DataIntegrityProbe : IDataIntegrityProbe
//{
//    public Task<LiveIntegrityProbeSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
//    {
//        var snapshot = new LiveIntegrityProbeSnapshot(
//            MediaAssetCount: 0,
//            MediaFileCount: 0,
//            DownloadEntitlementCount: 0,
//            InstalledPluginCount: 0,
//            PluginFolderCount: 0,
//            SettingsCount: 0,
//            MigrationVersions: Array.Empty<string>(),
//            InstalledPlugins: Array.Empty<string>());

//        return Task.FromResult(snapshot);
//    }
//}
