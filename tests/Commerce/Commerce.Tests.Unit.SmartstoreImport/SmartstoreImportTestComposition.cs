using Commerce.Catalog.Domain.Entities;
using Commerce.Cms.Domain.Entities;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Commerce.Framework.Data.Identity;
using Commerce.Media.Domain.Entities;
using Commerce.Orders.Domain.Entities;
using Commerce.Pricing.Domain.Entities;
using Commerce.Reviews.Domain.Entities;
using Commerce.Seo.Domain.Entities;
using Commerce.SmartstoreImport.Domain.Entities;
using Commerce.SmartstoreImport.Infrastructure.DependencyInjection;
using Commerce.SmartstoreImport.Infrastructure.Persistence;
using Commerce.Store.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderEntity = Commerce.Orders.Domain.Entities.Order;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Tests.Unit.SmartstoreImport;

internal static class SmartstoreImportTestComposition
{
    public static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<CommerceDataOptions>();
        services.AddIdentity<CommerceIdentityUser, CommerceIdentityRole>()
            .AddEntityFrameworkStores<CommerceDbContext>()
            .AddDefaultTokenProviders();
        services.AddSingleton<ICommerceModelContributor, SmartstoreImportTestModelContributor>();
        services.AddSingleton<ICommerceModelContributor, SmartstoreImportModelContributor>();
        services.AddSingleton<ICommerceDbContextConfigurator, InMemorySmartstoreImportDbContextConfigurator>();
        services.AddSmartstoreImportInfrastructure(new ConfigurationBuilder().Build());
        services.AddCommerceDbContext();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<CommerceDbContext>().Database.EnsureCreated();
        return provider;
    }

    public static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    private sealed class InMemorySmartstoreImportDbContextConfigurator : ICommerceDbContextConfigurator
    {
        private readonly string _databaseName = Guid.NewGuid().ToString("N");

        public void Configure(DbContextOptionsBuilder optionsBuilder, CommerceDataOptions dataOptions) =>
            optionsBuilder.UseInMemoryDatabase(_databaseName);
    }

    private sealed class SmartstoreImportTestModelContributor : ICommerceModelContributor
    {
        public void ConfigureModel(ModelBuilder modelBuilder)
        {
            ConfigureRoot<StoreEntity>(modelBuilder);
            ConfigureRoot<Language>(modelBuilder);
            ConfigureRoot<StoreCurrency>(modelBuilder);
            modelBuilder.Entity<Setting>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd();
            });
            ConfigureRoot<Customer>(modelBuilder);
            ConfigureRoot<Category>(modelBuilder);
            ConfigureRoot<Product>(modelBuilder);
            ConfigureRoot<ProductOffer>(modelBuilder);
            ConfigureEntity<ProductCategory>(modelBuilder);
            ConfigureEntity<ProductMedia>(modelBuilder);
            ConfigureRoot<MediaAsset>(modelBuilder);
            ConfigureOrder(modelBuilder);
            ConfigureEntity<OrderItem>(modelBuilder);
            ConfigureRoot<Topic>(modelBuilder);
            ConfigureEntity<TopicLocalization>(modelBuilder);
            ConfigureRoot<UrlRecord>(modelBuilder);
            ConfigureEntity<EntityTranslation>(modelBuilder);
            ConfigureRoot<Discount>(modelBuilder);
            ConfigureRoot<ProductReview>(modelBuilder);
            ConfigureRoot<ImportRun>(modelBuilder);
            ConfigureEntity<ImportIdMapping>(modelBuilder);
            ConfigureEntity<ImportIssue>(modelBuilder);
        }

        private static void ConfigureRoot<T>(ModelBuilder modelBuilder) where T : Commerce.Framework.Core.Entities.AggregateRoot
        {
            var builder = modelBuilder.Entity<T>();
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Ignore(x => x.DomainEvents);
        }

        private static void ConfigureEntity<T>(ModelBuilder modelBuilder) where T : Commerce.Framework.Core.Entities.Entity
        {
            var builder = modelBuilder.Entity<T>();
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }

        private static void ConfigureOrder(ModelBuilder modelBuilder)
        {
            var builder = modelBuilder.Entity<OrderEntity>();
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.Items);
            builder.Ignore(x => x.StatusHistory);
            builder.Ignore(x => x.TaxLines);
            builder.Ignore(x => x.BillingAddress);
            builder.Ignore(x => x.ShippingAddress);
        }
    }
}
