using Commerce.Cart.Infrastructure.DependencyInjection;
using Commerce.Cart.Infrastructure.Migrations;
using Commerce.Cart.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Cart;

public sealed class CartModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.cart",
        SystemName: "Commerce.Cart",
        Name: "Cart",
        Version: new Version(1, 0, 0),
        Description: "Shopping cart module.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Inventory")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, CartModelContributor>();
        services.AddSingleton<ICommerceMigration, CartInitialMigration>();
        services.AddCartInfrastructure();
    }
}
