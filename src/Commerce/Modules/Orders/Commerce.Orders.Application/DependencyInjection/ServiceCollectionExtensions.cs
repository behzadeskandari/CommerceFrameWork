using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Application.Integration;
using Commerce.Orders.Application.Orders;
using Commerce.Orders.Contracts.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Orders.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminOrderService, OrderService>();
        services.AddScoped<IOrderLifecycleService, Lifecycle.OrderLifecycleService>();
        services.AddScoped<IReturnAdminService, Lifecycle.ReturnCaseService>();
        services.AddScoped<IOrderFulfillmentUpdater, OrderFulfillmentUpdater>();
        services.AddScoped<IOrderNotificationReader, OrderNotificationReader>();
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddSingleton<IOrderAccessTokenGenerator, OrderAccessTokenGenerator>();
        services.AddScoped<IOrderCreatedHandler, OrderWalletConsumptionHandler>();
        services.AddScoped<IOrderPaidHandler, OrderPaidAffiliateCommissionHandler>();
        return services;
    }
}
