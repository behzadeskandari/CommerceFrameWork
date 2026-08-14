using Commerce.Promotions.Infrastructure.DependencyInjection;
using Commerce.Promotions.Infrastructure.Migrations;
using Commerce.Promotions.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Promotions;

public sealed class PromotionsModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.promotions",
        SystemName: "Commerce.Promotions",
        Name: "Promotions",
        Version: new Version(1, 0, 0),
        Description: "Rule-based marketing promotions.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Pricing"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Customers")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, PromotionsModelContributor>();
        services.AddSingleton<ICommerceMigration, PromotionsInitialMigration>();
        services.AddPromotionsInfrastructure();
    }
}
