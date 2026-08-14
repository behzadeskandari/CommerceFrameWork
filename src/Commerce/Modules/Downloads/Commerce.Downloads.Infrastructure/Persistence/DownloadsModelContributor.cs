using Commerce.Framework.Data.Db;
using Commerce.Downloads.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Downloads.Infrastructure.Persistence;

public sealed class DownloadsModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductDownloadSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new ProductDownloadFileConfiguration());
        modelBuilder.ApplyConfiguration(new DownloadEntitlementConfiguration());
        modelBuilder.ApplyConfiguration(new DownloadHistoryEntryConfiguration());
    }
}
