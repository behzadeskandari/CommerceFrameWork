using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Application.DependencyInjection;
using Commerce.Inventory.Infrastructure.Migrations;
using Commerce.Inventory.Infrastructure.Persistence;
using Commerce.Inventory.Infrastructure.Persistence.Repositories;
using Commerce.Inventory.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Inventory.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, InventoryPermissionContributor>();
        services.AddScoped<IInventoryRepository, EfInventoryRepository>();
        services.AddInventoryApplication();
        return services;
    }
}
