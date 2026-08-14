using Commerce.Cms.Application.DependencyInjection;
using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Infrastructure.Persistence.Repositories;
using Commerce.Cms.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Cms.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCmsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, CmsPermissionContributor>();
        services.AddScoped<ICmsRepository, EfCmsRepository>();
        services.AddCmsApplication();
        return services;
    }
}
