using Commerce.Checkout.Contracts.Checkout;
using Commerce.Orders.Contracts.Orders;
using Commerce.Shipping.Application.Abstractions;
using Commerce.Shipping.Application.Shipments;
using Commerce.Shipping.Application.Shipping;
using Commerce.Shipping.Contracts.Admin;
using Commerce.Shipping.Contracts.Shipments;
using Commerce.Shipping.Contracts.Shipping;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Shipping.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShippingApplication(this IServiceCollection services)
    {
        services.AddScoped<IShippingAdminService, ShippingAdminService>();
        services.AddScoped<IShippingCalculationService, ShippingCalculationService>();
        services.AddScoped<IShipmentAdminService, ShipmentAdminService>();
        services.AddScoped<IOrderFulfillmentSync, OrderFulfillmentSync>();
        services.AddScoped<IShippingProviderRegistry, ShippingProviderRegistry>();
        services.AddScoped<ShippingProviderResolver>();
        services.AddScoped<ShippingSettings>();
        services.AddScoped<IShippingProvider, FlatRateShippingProvider>();
        services.AddScoped<IShippingProvider, PickupShippingProvider>();
        services.AddScoped<IShippingRateProvider, FlatRateShippingRateProvider>();
        services.AddScoped<IShippingRateProvider, PickupShippingRateProvider>();
        return services;
    }
}
