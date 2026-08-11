using Commerce.Checkout.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Checkout.Infrastructure.Persistence;

public sealed class CheckoutModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CheckoutSessionConfiguration());
        modelBuilder.ApplyConfiguration(new CheckoutSessionItemConfiguration());
    }
}
