using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Data.Tenancy;

public interface IStoreContextInitializerService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class StoreContextInitializerService(
    CommerceDbContext dbContext,
    IStoreContextAccessor accessor,
    ILogger<StoreContextInitializerService> logger) : IStoreContextInitializerService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var store = await dbContext.BootstrapStores
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (store is null)
        {
            logger.LogWarning("No active bootstrap store found for store context initialization.");
            return;
        }

        accessor.SetStore(store.Id, store.Name);
        logger.LogInformation("Store context initialized for store {StoreId} ({StoreName}).", store.Id, store.Name);
    }
}
