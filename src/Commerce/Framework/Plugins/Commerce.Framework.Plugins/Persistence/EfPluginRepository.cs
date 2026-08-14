using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Framework.Plugins.Persistence;

public sealed class EfPluginRepository(CommerceDbContext dbContext)
{
    public async Task<IReadOnlyList<CommercePluginInstallation>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<CommercePluginInstallation>()
            .AsNoTracking()
            .OrderBy(x => x.SystemName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<CommercePluginInstallation?> FindBySystemNameAsync(
        string systemName,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<CommercePluginInstallation>()
            .FirstOrDefaultAsync(x => x.SystemName == systemName, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CommercePluginInstallation>> GetEnabledAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<CommercePluginInstallation>()
            .AsNoTracking()
            .Where(x => x.IsInstalled && x.IsEnabled)
            .OrderBy(x => x.SystemName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(CommercePluginInstallation installation, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CommercePluginInstallation>().Add(installation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    public async Task RemoveAsync(CommercePluginInstallation installation, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CommercePluginInstallation>().Remove(installation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
