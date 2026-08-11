using Commerce.Cart.Application.Abstractions;
using Commerce.Cart.Application.Carts;
using Commerce.Cart.Contracts.Carts;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Cart.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartApplication(this IServiceCollection services)
    {
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICartConversionService, CartConversionService>();
        services.AddScoped<ICartOfferValidator, CartOfferValidator>();
        services.AddScoped<ICartTotalsCalculator, CartTotalsCalculator>();
        services.AddScoped<ICartItemDisplayEnricher, CartItemDisplayEnricher>();
        services.AddScoped<CartSettings>();
        return services;
    }
}
