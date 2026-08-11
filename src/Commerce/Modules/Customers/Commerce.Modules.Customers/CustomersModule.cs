using Commerce.Customers.Application.DependencyInjection;
using Commerce.Customers.Infrastructure.DependencyInjection;
using Commerce.Customers.Infrastructure.Migrations;
using Commerce.Customers.Infrastructure.Persistence;
using Commerce.Customers.Infrastructure.Seeding;
using Commerce.Customers.Infrastructure.Security;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Customers;

public sealed class CustomersModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.customers",
        SystemName: "Commerce.Customers",
        Name: "Customers",
        Version: new Version(1, 0, 0),
        Description: "Customer registration, authentication, and profile management.",
        Dependencies: [new ModuleDependency("Commerce.Core")],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, CustomersModelContributor>();
        services.AddSingleton<ICommerceMigration, CustomersInitialMigration>();
        services.AddSingleton<ICommerceSeeder, CustomersIdentitySeeder>();
        services.AddSingleton<IModulePermissionContributor, CustomersPermissionContributor>();
        services.AddScoped<IAdministratorProvisioningService, AdministratorProvisioningService>();
        services.AddCustomersApplication();
        services.AddCustomersInfrastructure();
    }
}
