using Commerce.Cart.Application.Abstractions;
using Commerce.Cart.Application.DependencyInjection;
using Commerce.Cart.Infrastructure.Configuration;
using Commerce.Cart.Contracts.Carts;
using Commerce.Cart.Infrastructure.Cookies;
using Commerce.Cart.Infrastructure.Persistence.Repositories;
using Commerce.Framework.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Cart.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISettingDefinitionProvider, CartSettingDefinitionProvider>();
        services.AddScoped<ICartRepository, EfCartRepository>();
        services.AddScoped<GuestCartCookieManager>();
        services.AddScoped<IGuestCartCookieManager>(sp => sp.GetRequiredService<GuestCartCookieManager>());
        services.AddScoped<IGuestCartContext>(sp => sp.GetRequiredService<GuestCartCookieManager>());
        services.AddSingleton<ICartGuestTokenGenerator, CartGuestTokenGenerator>();
        services.AddCartApplication();
        return services;
    }
}
