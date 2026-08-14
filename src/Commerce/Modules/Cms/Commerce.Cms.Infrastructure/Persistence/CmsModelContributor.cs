using Commerce.Cms.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Cms.Infrastructure.Persistence;

public sealed class CmsModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContentPageConfiguration());
        modelBuilder.ApplyConfiguration(new ContentPageLocalizationConfiguration());
        modelBuilder.ApplyConfiguration(new TopicConfiguration());
        modelBuilder.ApplyConfiguration(new TopicLocalizationConfiguration());
        modelBuilder.ApplyConfiguration(new WidgetZoneConfiguration());
        modelBuilder.ApplyConfiguration(new WidgetInstanceConfiguration());
        modelBuilder.ApplyConfiguration(new MenuConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemLocalizationConfiguration());
    }
}
