using Commerce.Framework.Data.Db;
using Commerce.Shipping.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Shipping.Infrastructure.Persistence;

public sealed class ShippingModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ShippingMethodConfiguration());
        modelBuilder.ApplyConfiguration(new ShippingZoneConfiguration());
        modelBuilder.ApplyConfiguration(new ShippingZoneCountryConfiguration());
        modelBuilder.ApplyConfiguration(new ShippingZoneStateConfiguration());
        modelBuilder.ApplyConfiguration(new ShippingZonePostalRuleConfiguration());
        modelBuilder.ApplyConfiguration(new ShippingRateConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentItemConfiguration());
    }
}
