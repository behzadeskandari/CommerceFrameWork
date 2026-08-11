using Commerce.Inventory.Application.Inventory;
using Commerce.Inventory.Contracts.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Inventory.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddSingleton<InventorySettings>();
        services.AddScoped<IInventoryReader, InventoryReader>();
        services.AddScoped<IStorefrontInventoryReader, InventoryReader>();
        services.AddScoped<IInventoryAdminService, InventoryAdminService>();
        services.AddScoped<IInventoryOrderService, InventoryOrderService>();
        services.AddScoped<IInventoryReservationService, InventoryReservationService>();
        services.AddScoped<IInventoryReservationExpirationService, InventoryReservationExpirationService>();
        return services;
    }
}
