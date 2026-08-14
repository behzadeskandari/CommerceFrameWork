using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Framework.Plugins.Persistence;

public sealed class EfPluginStoreConfigurationRepository(CommerceDbContext dbContext)
{
    public async Task<CommercePluginStoreConfiguration?> FindAsync(
        string pluginSystemName,
        int storeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<CommercePluginStoreConfiguration>()
            .FirstOrDefaultAsync(
                x => x.PluginSystemName == pluginSystemName && x.StoreId == storeId,
                cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CommercePluginStoreConfiguration>> ListForPluginAsync(
        string pluginSystemName,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<CommercePluginStoreConfiguration>()
            .AsNoTracking()
            .Where(x => x.PluginSystemName == pluginSystemName)
            .OrderBy(x => x.StoreId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(
        CommercePluginStoreConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindAsync(configuration.PluginSystemName, configuration.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.Set<CommercePluginStoreConfiguration>().Add(configuration);
        }
        else
        {
            existing.SetEnabled(configuration.IsEnabled);
            existing.SetConfiguration(configuration.ConfigurationJson);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveForPluginAsync(string pluginSystemName, CancellationToken cancellationToken = default)
    {
        var configurations = await dbContext.Set<CommercePluginStoreConfiguration>()
            .Where(x => x.PluginSystemName == pluginSystemName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (configurations.Count == 0)
        {
            return;
        }

        dbContext.Set<CommercePluginStoreConfiguration>().RemoveRange(configurations);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
