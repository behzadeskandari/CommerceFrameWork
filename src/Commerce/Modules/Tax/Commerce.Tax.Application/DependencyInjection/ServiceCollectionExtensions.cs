using Commerce.Checkout.Contracts.Checkout;
using Commerce.Tax.Application.Tax;
using Commerce.Tax.Contracts.Admin;
using Commerce.Tax.Contracts.Tax;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Tax.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaxApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaxAdminService, TaxAdminService>();
        services.AddScoped<ITaxCalculationService, TaxCalculationService>();
        services.AddScoped<ITaxProvider, InternalTaxProvider>();
        services.AddScoped<ITaxCalculator, CheckoutTaxCalculator>();
        services.AddScoped<TaxSettings>();
        return services;
    }
}

