using Commerce.Themes.Application;
using Commerce.Themes.Contracts;
using Commerce.Themes.Contracts.Admin;
using Commerce.Themes.Contracts.Storefront;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Themes.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThemesApplication(this IServiceCollection services)
    {
        services.AddScoped<IThemeAdminService, ThemeAdminService>();
        services.AddScoped<IThemeStorefrontService, ThemeStorefrontService>();
        return services;
    }
}
