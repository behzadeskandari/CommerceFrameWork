using Commerce.Integration.Application.Abstractions;
using Commerce.Integration.Application.ApiClients;
using Commerce.Integration.Application.Events;
using Commerce.Integration.Application.Jobs;
using Commerce.Integration.Application.ExternalApi;
using Commerce.Integration.Application.Webhooks;
using Commerce.Integration.Contracts.ApiClients;
using Commerce.Integration.Contracts.ExternalApi;
using Commerce.Integration.Contracts.Events;
using Commerce.Framework.Scheduling;
using Commerce.Integration.Contracts.Webhooks;
using Commerce.Integration.Infrastructure.Persistence.Repositories;
using Commerce.Integration.Infrastructure.Security;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Events;
using Commerce.Orders.Contracts.Orders;
using Commerce.Customers.Contracts.Customers;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Integration.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, IntegrationPermissionContributor>();
        services.AddScoped<IIntegrationRepository, EfIntegrationRepository>();

        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
        services.AddScoped<IIntegrationEventIdempotencyService, IntegrationEventIdempotencyService>();
        services.AddScoped<IWebhookAdminService, WebhookAdminService>();
        services.AddScoped<IWebhookDeliveryProcessor, WebhookDeliveryProcessor>();
        services.AddSingleton<IWebhookSignatureService, WebhookSignatureService>();
        services.AddScoped<IApiClientAdminService, ApiClientAdminService>();
        services.AddScoped<IApiClientAuthenticator, ApiClientAuthenticator>();
        services.AddScoped<IExternalOrderService, ExternalOrderService>();

        services.AddScoped<IIntegrationEventHandler, WebhookDispatchIntegrationHandler>();
        services.AddScoped<IDomainEventIntegrationMapper, CatalogInventoryDomainEventMapper>();
        services.AddScoped<IBackgroundJobHandler, WebhookDeliveryJobHandler>();

        services.AddScoped<IOrderCreatedHandler, IntegrationOrderCreatedHandler>();
        services.AddScoped<IOrderPaidHandler, IntegrationOrderPaidHandler>();
        services.AddScoped<IOrderCancelledHandler, IntegrationOrderCancelledHandler>();
        services.AddScoped<IOrderPaymentFailedHandler, IntegrationOrderPaymentFailedHandler>();
        services.AddScoped<IOrderRefundHandler, IntegrationOrderRefundHandler>();
        services.AddScoped<IShipmentCreatedHandler, IntegrationShipmentCreatedHandler>();
        services.AddScoped<ICustomerRegisteredHandler, IntegrationCustomerRegisteredHandler>();

        services.AddHttpClient("Commerce.Webhooks")
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

        return services;
    }
}
