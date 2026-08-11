using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Store.Application.DependencyInjection;
using Commerce.Store.Infrastructure.DependencyInjection;
using Commerce.Store.Infrastructure.Migrations;
using Commerce.Store.Infrastructure.Persistence;
using Commerce.Store.Infrastructure.Security;
using Commerce.Store.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Store;

public sealed class StoreModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.store",
        SystemName: "Commerce.Store",
        Name: "Store",
        Version: new Version(1, 0, 0),
        Description: "Multi-store tenancy, languages, currencies, and settings.",
        Dependencies: [new ModuleDependency("Commerce.Core")],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, StoreModelContributor>();
        services.AddSingleton<ICommerceMigration, StoreInitialMigration>();
        services.AddSingleton<ICommerceSeeder, StoreIdentitySeeder>();
        services.AddSingleton<IModulePermissionContributor, StorePermissionContributor>();
        services.AddStoreApplication();
        services.AddStoreInfrastructure();
    }
}
