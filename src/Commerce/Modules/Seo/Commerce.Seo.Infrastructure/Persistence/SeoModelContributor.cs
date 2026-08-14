using Commerce.Framework.Data.Db;
using Commerce.Seo.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Seo.Infrastructure.Persistence;

public sealed class SeoModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UrlRecordConfiguration());
        modelBuilder.ApplyConfiguration(new SeoMetadataConfiguration());
        modelBuilder.ApplyConfiguration(new SeoSettingsConfiguration());
    }
}
