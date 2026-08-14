using Commerce.Seo.Application.Abstractions;
using Commerce.Seo.Application.Admin;
using Commerce.Seo.Application.Storefront;
using Commerce.Seo.Contracts.Admin;
using Commerce.Seo.Contracts.Storefront;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Seo.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSeoApplication(this IServiceCollection services)
    {
        services.AddScoped<ISeoAdminService, SeoAdminService>();
        services.AddScoped<ISeoStorefrontService, SeoStorefrontService>();
        return services;
    }
}
