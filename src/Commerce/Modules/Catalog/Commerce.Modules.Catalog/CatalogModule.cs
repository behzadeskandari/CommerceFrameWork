using Commerce.Catalog.Application.DependencyInjection;
using Commerce.Catalog.Infrastructure.DependencyInjection;
using Commerce.Catalog.Infrastructure.Migrations;
using Commerce.Catalog.Infrastructure.Persistence;
using Commerce.Catalog.Infrastructure.Seeding;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Catalog;

public sealed class CatalogModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.catalog",
        SystemName: "Commerce.Catalog",
        Name: "Catalog",
        Version: new Version(1, 0, 0),
        Description: "Product and category catalog module.",
        Dependencies: [new ModuleDependency("Commerce.Core")],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, CatalogModelContributor>();
        services.AddSingleton<ICommerceMigration, CatalogInitialMigration>();
        services.AddSingleton<ICommerceSeeder, CatalogDevelopmentSeeder>();
        services.AddCatalogApplication();
        services.AddCatalogInfrastructure();
    }
}
