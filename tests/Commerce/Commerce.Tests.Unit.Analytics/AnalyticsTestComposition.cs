using Commerce.Analytics.Infrastructure.DependencyInjection;
using Commerce.Cart.Domain.Entities;
using Commerce.Catalog.Domain.Entities;
using Commerce.Checkout.Domain.Entities;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Inventory.Domain.Entities;
using Commerce.Orders.Domain.Entities;
using Commerce.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Tests.Unit.Analytics;

internal sealed class AnalyticsTestModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.BillingAddress);
            builder.Ignore(x => x.ShippingAddress);
            builder.Ignore(x => x.Items);
            builder.Ignore(x => x.StatusHistory);
            builder.Ignore(x => x.TaxLines);
        });

        modelBuilder.Entity<OrderItem>(builder => builder.HasKey(x => x.Id));

        modelBuilder.Entity<Payment>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.Transactions);
            builder.Ignore(x => x.Attempts);
            builder.Ignore(x => x.Refunds);
        });

        modelBuilder.Entity<Refund>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.Transactions);
        });

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<ShoppingCart>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.Items);
        });

        modelBuilder.Entity<CheckoutSession>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.BillingAddress);
            builder.Ignore(x => x.ShippingAddress);
            builder.Ignore(x => x.Items);
        });

        modelBuilder.Entity<CheckoutSessionItem>(builder => builder.HasKey(x => x.Id));

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<InventoryItem>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.DomainEvents);
        });
    }
}

internal static class AnalyticsTestComposition
{
    public static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<CommerceDataOptions>();
        services.AddSingleton<ICommerceModelContributor, AnalyticsTestModelContributor>();
        services.AddSingleton<ICommerceDbContextConfigurator, InMemoryAnalyticsDbContextConfigurator>();
        services.AddAnalyticsInfrastructure();
        services.AddCommerceDbContext();
        return services.BuildServiceProvider();
    }

    private sealed class InMemoryAnalyticsDbContextConfigurator : ICommerceDbContextConfigurator
    {
        private readonly string _databaseName = Guid.NewGuid().ToString("N");

        public void Configure(DbContextOptionsBuilder optionsBuilder, CommerceDataOptions dataOptions) =>
            optionsBuilder.UseInMemoryDatabase(_databaseName);
    }
}
