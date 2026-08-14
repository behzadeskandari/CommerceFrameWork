using Commerce.Themes.Application.Abstractions;
using Commerce.Themes.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Themes.Infrastructure.Persistence.Repositories;

public sealed class EfThemeRepository(CommerceDbContext dbContext) : IThemeRepository
{
    public Task<StoreThemeConfiguration?> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<StoreThemeConfiguration>().FirstOrDefaultAsync(x => x.StoreId == storeId, cancellationToken);

    public async Task SaveAsync(StoreThemeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration.Id == 0)
        {
            dbContext.Set<StoreThemeConfiguration>().Add(configuration);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
