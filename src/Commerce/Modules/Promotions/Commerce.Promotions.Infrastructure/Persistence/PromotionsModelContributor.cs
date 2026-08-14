using Commerce.Framework.Data.Db;
using Commerce.Promotions.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Promotions.Infrastructure.Persistence;

public sealed class PromotionsModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PromotionConfiguration());
        modelBuilder.ApplyConfiguration(new PromotionConditionConfiguration());
        modelBuilder.ApplyConfiguration(new PromotionActionConfiguration());
        modelBuilder.ApplyConfiguration(new PromotionUsageConfiguration());
    }
}
