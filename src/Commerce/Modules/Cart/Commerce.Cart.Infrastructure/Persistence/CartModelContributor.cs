using Commerce.Framework.Data.Db;
using Commerce.Cart.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Cart.Infrastructure.Persistence;

public sealed class CartModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ShoppingCartConfiguration());
        modelBuilder.ApplyConfiguration(new CartItemConfiguration());
    }
}
