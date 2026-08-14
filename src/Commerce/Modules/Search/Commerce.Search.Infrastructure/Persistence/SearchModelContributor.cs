using Commerce.Search.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Search.Infrastructure.Persistence;

public sealed class SearchModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SearchIndexEntryConfiguration());
        modelBuilder.ApplyConfiguration(new SearchIndexJobConfiguration());
    }
}
