using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Data.Tenancy;

public interface IStoreContextInitializerService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class StoreContextInitializerService(
    IServiceProvider serviceProvider,
    IStoreContextAccessor accessor,
    CommerceDbContext dbContext,
    ILogger<StoreContextInitializerService> logger) : IStoreContextInitializerService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var bootstrap = serviceProvider.GetService<IStoreContextBootstrap>();
        if (bootstrap is not null)
        {
            await bootstrap.InitializeAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Store context initialized via store module bootstrap.");
            return;
        }

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

        accessor.SetStore(store.Id, store.Name, store.Name);
        logger.LogInformation("Store context initialized for bootstrap store {StoreId}.", store.Id);
    }
}
