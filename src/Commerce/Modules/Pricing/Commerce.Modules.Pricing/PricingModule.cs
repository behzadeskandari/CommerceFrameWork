using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Pricing.Infrastructure.DependencyInjection;
using Commerce.Pricing.Infrastructure.Migrations;
using Commerce.Pricing.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Pricing;

public sealed class PricingModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.pricing",
        SystemName: "Commerce.Pricing",
        Name: "Pricing",
        Version: new Version(1, 0, 0),
        Description: "Pricing rules, discounts, and coupon engine.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Customers")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, PricingModelContributor>();
        services.AddSingleton<ICommerceMigration, PricingInitialMigration>();
        services.AddSingleton<ICommerceMigration, PricingPhase21Migration>();
        services.AddPricingInfrastructure();
    }
}
