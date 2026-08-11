using Commerce.Catalog.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Catalog.Infrastructure.Persistence;

public sealed class CatalogModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new CatalogProductConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductAttributeDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductAttributeValueConfiguration());
    }
}
