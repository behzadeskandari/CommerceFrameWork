using Commerce.Framework.Data.Db;
using Commerce.Inventory.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Inventory.Infrastructure.Persistence;

public sealed class InventoryModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryMovementConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryReservationConfiguration());
        modelBuilder.ApplyConfiguration(new WarehouseConfiguration());
        modelBuilder.ApplyConfiguration(new StockLocationConfiguration());
    }
}
