using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Application.Admin;
using Commerce.Notifications.Application.Channels;
using Commerce.Notifications.Application.Dispatch;
using Commerce.Notifications.Application.Handlers;
using Commerce.Notifications.Application.Jobs;
using Commerce.Notifications.Contracts.Admin;
using Commerce.Notifications.Contracts.Dispatch;
using Commerce.Notifications.Contracts.Storefront;
using Commerce.Framework.Scheduling;
using Commerce.Customers.Contracts.Customers;
using Commerce.Downloads.Contracts.Downloads;
using Commerce.Orders.Contracts.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Notifications.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationChannelProvider, EmailNotificationChannelProvider>();
        services.AddScoped<INotificationChannelProvider, SmsNotificationChannelProvider>();
        services.AddScoped<INotificationChannelProvider, InAppNotificationChannelProvider>();

        services.AddScoped<NotificationDispatcher>();
        services.AddScoped<INotificationEventPublisher, NotificationEventPublisher>();
        services.AddScoped<INotificationTemplateAdminService, NotificationTemplateAdminService>();
        services.AddScoped<INotificationHistoryAdminService, NotificationHistoryAdminService>();
        services.AddScoped<IInAppNotificationStorefrontService, InAppNotificationStorefrontService>();

        services.AddScoped<OrderCreatedNotificationHandler>();
        services.AddScoped<ICustomerRegisteredHandler, CustomerRegisteredNotificationHandler>();
        services.AddScoped<IOrderCreatedHandler, OrderCreatedNotificationHandler>();
        services.AddScoped<IOrderPaidHandler, OrderPaidNotificationHandler>();
        services.AddScoped<IOrderPaymentFailedHandler, OrderPaymentFailedNotificationHandler>();
        services.AddScoped<IOrderCancelledHandler, OrderCancelledNotificationHandler>();
        services.AddScoped<IOrderRefundHandler, OrderRefundNotificationHandler>();
        services.AddScoped<IOrderReturnHandler, OrderReturnNotificationHandler>();
        services.AddScoped<IShipmentCreatedHandler, ShipmentCreatedNotificationHandler>();
        services.AddScoped<IDownloadAvailableHandler, DownloadAvailableNotificationHandler>();

        services.AddScoped<IBackgroundJobHandler, NotificationRetryJobHandler>();
        services.AddScoped<IBackgroundJobHandler, NotificationDeliverJobHandler>();

        return services;
    }
}
