using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Shipping.Infrastructure.DependencyInjection;
using Commerce.Shipping.Infrastructure.Migrations;
using Commerce.Shipping.Infrastructure.Persistence;
using Commerce.Shipping.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Shipping;

public sealed class ShippingModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.shipping",
        SystemName: "Commerce.Shipping",
        Name: "Shipping",
        Version: new Version(1, 0, 0),
        Description: "Shipping methods, zones, rates, and calculation engine.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Checkout")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, ShippingModelContributor>();
        services.AddSingleton<ICommerceMigration, ShippingInitialMigration>();
        services.AddSingleton<ICommerceSeeder, ShippingDevelopmentSeeder>();
        services.AddShippingInfrastructure();
    }
}
