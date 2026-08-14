using Commerce.Downloads.Application.Abstractions;
using Commerce.Downloads.Contracts.Admin;
using Commerce.Downloads.Contracts.Storefront;
using Commerce.Downloads.Application.Admin;
using Commerce.Downloads.Application.Entitlements;
using Commerce.Downloads.Application.Storefront;
using Commerce.Orders.Contracts.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Downloads.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDownloadsApplication(this IServiceCollection services)
    {
        services.AddScoped<IDownloadAdminService, DownloadAdminService>();
        services.AddScoped<ICustomerDownloadService, CustomerDownloadService>();
        services.AddScoped<IDownloadEntitlementService, DownloadEntitlementService>();
        services.AddScoped<IOrderPaidHandler, DownloadEntitlementGrantHandler>();
        return services;
    }
}
