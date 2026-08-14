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
        modelBuilder.ApplyConfiguration(new CatalogProductAttributeOptionConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductAttributeAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductAttributeValueConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductVariantConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductVariantAttributeConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductOfferConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogOfferTierPriceConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductMediaConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductVariantMediaConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogCategoryMediaConfiguration());
    }
}
