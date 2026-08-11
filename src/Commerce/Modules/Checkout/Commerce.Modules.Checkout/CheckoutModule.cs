using Commerce.Checkout.Infrastructure.DependencyInjection;
using Commerce.Checkout.Infrastructure.Migrations;
using Commerce.Checkout.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Checkout;

public sealed class CheckoutModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.checkout",
        SystemName: "Commerce.Checkout",
        Name: "Checkout",
        Version: new Version(1, 0, 0),
        Description: "Checkout orchestration module.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Cart"),
            new ModuleDependency("Commerce.Inventory")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, CheckoutModelContributor>();
        services.AddSingleton<ICommerceMigration, CheckoutInitialMigration>();
        services.AddCheckoutInfrastructure();
    }
}
