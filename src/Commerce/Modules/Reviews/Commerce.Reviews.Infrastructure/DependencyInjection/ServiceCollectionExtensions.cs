using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Application.DependencyInjection;
using Commerce.Reviews.Infrastructure.Persistence.Repositories;
using Commerce.Reviews.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Reviews.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReviewsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, ReviewPermissionContributor>();
        services.AddScoped<IReviewsRepository, EfReviewsRepository>();
        services.AddReviewsApplication();
        return services;
    }
}
