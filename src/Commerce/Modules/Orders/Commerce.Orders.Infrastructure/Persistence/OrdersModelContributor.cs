using Commerce.Framework.Data.Db;
using Commerce.Orders.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Orders.Infrastructure.Persistence;

public sealed class OrdersModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new OrderStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new OrderCreationIdempotencyConfiguration());
        modelBuilder.ApplyConfiguration(new StoreOrderNumberSequenceConfiguration());
    }
}
