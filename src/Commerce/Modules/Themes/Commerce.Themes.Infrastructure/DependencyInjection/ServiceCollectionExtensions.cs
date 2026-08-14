using Commerce.Themes.Application.Abstractions;
using Commerce.Themes.Application.DependencyInjection;
using Commerce.Themes.Infrastructure.Persistence.Repositories;
using Commerce.Themes.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Themes.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThemesInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, ThemePermissionContributor>();
        services.AddScoped<IThemeRepository, EfThemeRepository>();
        services.AddThemesApplication();
        return services;
    }
}
