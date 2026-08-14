using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Application.DependencyInjection;
using Commerce.Promotions.Infrastructure.Persistence.Repositories;
using Commerce.Promotions.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Promotions.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPromotionsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, PromotionPermissionContributor>();
        services.AddScoped<IPromotionsRepository, EfPromotionsRepository>();
        services.AddPromotionsApplication();
        return services;
    }
}
