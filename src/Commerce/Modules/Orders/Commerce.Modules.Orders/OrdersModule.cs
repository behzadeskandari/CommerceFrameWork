using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Orders.Infrastructure.DependencyInjection;
using Commerce.Orders.Infrastructure.Migrations;
using Commerce.Orders.Infrastructure.Persistence;
using Commerce.Orders.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Orders;

public sealed class OrdersModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.orders",
        SystemName: "Commerce.Orders",
        Name: "Orders",
        Version: new Version(1, 0, 0),
        Description: "Order engine with immutable commercial snapshots.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Cart"),
            new ModuleDependency("Commerce.Checkout"),
            new ModuleDependency("Commerce.Inventory"),
            new ModuleDependency("Commerce.Pricing")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, OrdersModelContributor>();
        services.AddSingleton<ICommerceMigration, OrdersInitialMigration>();
        services.AddOrdersInfrastructure();
    }
}
