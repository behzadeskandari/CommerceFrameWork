using Commerce.Store.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Store.Infrastructure.Persistence;

public sealed class StoreModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new StoreStoreConfiguration());
        modelBuilder.ApplyConfiguration(new StoreStoreDomainConfiguration());
        modelBuilder.ApplyConfiguration(new StoreLanguageConfiguration());
        modelBuilder.ApplyConfiguration(new StoreCurrencyConfiguration());
        modelBuilder.ApplyConfiguration(new StoreEntityTranslationConfiguration());
        modelBuilder.ApplyConfiguration(new StoreMediaConfiguration());
    }
}
