using Commerce.Framework.Data.Db;
using Commerce.Pricing.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Pricing.Infrastructure.Persistence;

public sealed class PricingModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DiscountConfiguration());
        modelBuilder.ApplyConfiguration(new DiscountTargetConfiguration());
        modelBuilder.ApplyConfiguration(new CouponConfiguration());
        modelBuilder.ApplyConfiguration(new CouponUsageConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerGroupConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerGroupPriceConfiguration());
    }
}
