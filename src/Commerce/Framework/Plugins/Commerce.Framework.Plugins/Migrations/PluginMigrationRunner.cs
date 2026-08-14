using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Plugins.Migrations;

public sealed class PluginMigrationRunner(
    CommerceDbContext dbContext,
    IServiceProvider serviceProvider,
    ILogger<PluginMigrationRunner> logger)
{
    public async Task<IReadOnlyList<PluginMigrationStatusDto>> GetStatusAsync(
        string pluginSystemName,
        IReadOnlyList<ICommerceMigration> migrations,
        CancellationToken cancellationToken = default)
    {
        var applied = await dbContext.MigrationVersionInfo
            .AsNoTracking()
            .Where(x => x.Module == pluginSystemName)
            .Select(x => x.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var appliedSet = applied.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return migrations
            .Select(migration => new PluginMigrationStatusDto(
                migration.Name,
                migration.Version,
                migration.Description,
                appliedSet.Contains(migration.Version)))
            .ToList();
    }

    public async Task RunPendingAsync(
        IReadOnlyList<ICommerceMigration> migrations,
        CancellationToken cancellationToken = default)
    {
        if (migrations.Count == 0)
        {
            return;
        }

        var applied = await dbContext.MigrationVersionInfo
            .AsNoTracking()
            .Where(x => x.Module == migrations[0].Module)
            .Select(x => x.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var appliedSet = applied.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var migration in migrations)
        {
            if (appliedSet.Contains(migration.Version))
            {
                continue;
            }

            logger.LogInformation(
                "Applying plugin migration {MigrationName} ({Module} v{Version})",
                migration.Name,
                migration.Module,
                migration.Version);

            var context = new MigrationExecutionContext(dbContext, serviceProvider);
            await migration.UpAsync(context, cancellationToken).ConfigureAwait(false);

            dbContext.MigrationVersionInfo.Add(new MigrationVersionInfo
            {
                Version = migration.Version,
                MigrationName = migration.Name,
                Module = migration.Module,
                AppliedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed record PluginMigrationStatusDto(
    string Name,
    string Version,
    string Description,
    bool IsApplied);
