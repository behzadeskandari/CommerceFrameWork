using Commerce.Inventory.Application.Inventory;
using Commerce.Inventory.Application.Integration;
using Commerce.Inventory.Application.Jobs;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Framework.Scheduling;
using Commerce.Orders.Contracts.Orders;
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
        services.AddScoped<IWarehouseAdminService, WarehouseAdminService>();
        services.AddScoped<IInventoryTransferService, InventoryTransferService>();
        services.AddScoped<IInventoryOrderService, InventoryOrderService>();
        services.AddScoped<IInventoryReservationService, InventoryReservationService>();
        services.AddScoped<IInventoryReservationExpirationService, InventoryReservationExpirationService>();
        services.AddScoped<IOrderPaidHandler, OrderPaidInventoryHandler>();
        services.AddScoped<IBackgroundJobHandler, InventoryReservationExpirationJobHandler>();
        return services;
    }
}
