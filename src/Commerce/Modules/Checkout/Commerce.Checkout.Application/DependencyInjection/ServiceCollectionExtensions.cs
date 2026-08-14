using Commerce.Checkout.Application.Abstractions;

using Commerce.Checkout.Application.Checkout;

using Commerce.Checkout.Application.Providers;

using Commerce.Checkout.Contracts.Checkout;

using Microsoft.Extensions.DependencyInjection;



namespace Commerce.Checkout.Application.DependencyInjection;



public static class ServiceCollectionExtensions

{

    public static IServiceCollection AddCheckoutApplication(this IServiceCollection services)

    {

        services.AddScoped<CheckoutService>();
        services.AddScoped<ICheckoutService>(sp => sp.GetRequiredService<CheckoutService>());
        services.AddScoped<ICheckoutOrderPreparationService>(sp => sp.GetRequiredService<CheckoutService>());
        services.AddScoped<ICheckoutCompletionService, CheckoutCompletionService>();

        services.AddScoped<ICheckoutOfferValidator, CheckoutOfferValidator>();

        services.AddScoped<ICheckoutItemEnricher, CheckoutItemEnricher>();

        services.AddScoped<CheckoutRequiresShippingEvaluator>();

        services.AddScoped<ICheckoutTotalsCalculator, CheckoutTotalsCalculator>();
        services.AddScoped<ICheckoutWalletCalculator, CheckoutWalletCalculator>();

        services.AddSingleton<ITaxCalculator, NoOpTaxCalculator>();

        services.AddSingleton<IDiscountCalculator, NoOpDiscountCalculator>();

        services.AddSingleton<IPaymentMethodProvider, NoOpPaymentMethodProvider>();

        services.AddScoped<CheckoutSettings>();

        return services;

    }

}


