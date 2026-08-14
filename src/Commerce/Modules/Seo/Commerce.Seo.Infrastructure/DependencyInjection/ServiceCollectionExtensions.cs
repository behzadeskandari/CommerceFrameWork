using Commerce.Seo.Application.DependencyInjection;
using Commerce.Seo.Application.Abstractions;
using Commerce.Seo.Infrastructure.Persistence.Repositories;
using Commerce.Seo.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Seo.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSeoInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, SeoPermissionContributor>();
        services.AddScoped<ISeoRepository, EfSeoRepository>();
        services.AddSeoApplication();
        return services;
    }
}
