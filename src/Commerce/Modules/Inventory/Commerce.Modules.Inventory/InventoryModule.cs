using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Inventory.Infrastructure.DependencyInjection;
using Commerce.Inventory.Infrastructure.Migrations;
using Commerce.Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Inventory;

public sealed class InventoryModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.inventory",
        SystemName: "Commerce.Inventory",
        Name: "Inventory",
        Version: new Version(1, 0, 0),
        Description: "Stock, warehouses, reservations, and availability engine.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Scheduling")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, InventoryModelContributor>();
        services.AddSingleton<ICommerceMigration, InventoryInitialMigration>();
        services.AddInventoryInfrastructure();
    }

    public override async Task StartAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default)
    {
        await context.Services.RegisterInventoryRecurringJobsAsync(cancellationToken).ConfigureAwait(false);
    }
}
