using Commerce.Checkout.Application.Abstractions;
using Commerce.Checkout.Application.DependencyInjection;
using Commerce.Checkout.Infrastructure.Configuration;
using Commerce.Checkout.Infrastructure.Persistence.Repositories;
using Commerce.Framework.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Checkout.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheckoutInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISettingDefinitionProvider, CheckoutSettingDefinitionProvider>();
        services.AddScoped<ICheckoutRepository, EfCheckoutRepository>();
        services.AddCheckoutApplication();
        return services;
    }
}
