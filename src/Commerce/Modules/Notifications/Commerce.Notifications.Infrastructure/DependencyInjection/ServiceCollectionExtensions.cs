using Commerce.Notifications.Application.DependencyInjection;
using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Infrastructure.Persistence.Repositories;
using Commerce.Notifications.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Notifications.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, NotificationPermissionContributor>();
        services.AddScoped<INotificationsRepository, EfNotificationsRepository>();
        services.AddNotificationsApplication();
        return services;
    }
}
