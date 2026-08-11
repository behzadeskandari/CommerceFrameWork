using Commerce.Customers.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Customers.Infrastructure.Persistence;

public sealed class CustomersModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new CustomerCustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerAddressConfiguration());
    }
}
