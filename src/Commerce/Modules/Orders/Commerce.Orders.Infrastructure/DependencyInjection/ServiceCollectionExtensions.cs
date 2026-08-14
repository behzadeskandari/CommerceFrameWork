using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Application.DependencyInjection;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Infrastructure.Migrations;
using Commerce.Orders.Infrastructure.Persistence;
using Commerce.Orders.Infrastructure.Persistence.Repositories;
using Commerce.Orders.Infrastructure.Security;
using Commerce.Orders.Infrastructure.Transactions;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Orders.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, OrdersPermissionContributor>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IOrderPaymentSyncRepository>(sp => sp.GetRequiredService<EfOrderRepository>());
        services.AddScoped<IOrderPurchaseVerifier, OrderPurchaseVerifier>();
        services.AddScoped<IOrderNumberSequenceRepository, EfOrderNumberSequenceRepository>();
        services.AddScoped<IOrderCreationIdempotencyRepository, EfOrderCreationIdempotencyRepository>();
        services.AddScoped<IReturnCaseRepository, EfReturnCaseRepository>();
        services.AddScoped<IOrderCreationTransaction, OrderCreationTransaction>();
        services.AddOrdersApplication();
        return services;
    }
}
