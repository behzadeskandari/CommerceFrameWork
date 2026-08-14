using Commerce.Analytics.Application.Abstractions;
using Commerce.Analytics.Application.DependencyInjection;
using Commerce.Analytics.Infrastructure.Persistence.Repositories;
using Commerce.Analytics.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Analytics.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, AnalyticsPermissionContributor>();
        services.AddScoped<IAnalyticsReadRepository, EfAnalyticsReadRepository>();
        services.AddAnalyticsApplication();
        return services;
    }
}
