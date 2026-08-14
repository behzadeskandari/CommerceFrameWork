using Commerce.Catalog.Contracts.Pricing;
using Commerce.Checkout.Contracts.Checkout;
using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Application.AdvancedPricing;
using Commerce.Pricing.Application.Discounts;
using Commerce.Pricing.Application.Pricing;
using Commerce.Pricing.Contracts.AdvancedPricing;
using Commerce.Pricing.Contracts.Discounts;
using Commerce.Pricing.Contracts.Pricing;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Pricing.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPricingApplication(this IServiceCollection services)
    {
        services.AddScoped<IDiscountAdminService, DiscountAdminService>();
        services.AddScoped<ICouponAdminService, CouponAdminService>();
        services.AddScoped<IPriceCalculationService, PriceCalculationService>();
        services.AddScoped<IProductPricingPipeline, ProductPricingPipeline>();
        services.AddScoped<ICustomerGroupAdminService, CustomerGroupAdminService>();
        services.AddScoped<IAdvancedPricingService, AdvancedPricingService>();
        services.AddScoped<ICouponValidationService, CouponValidationService>();
        services.AddScoped<ICouponUsageService, CouponUsageService>();
        services.AddScoped<IProductCategoryLookup, ProductCategoryLookup>();
        services.AddScoped<IDiscountCalculator, CheckoutDiscountCalculator>();
        services.AddScoped<IPricingService, DiscountAwarePricingService>();
        services.AddScoped<ICatalogPricingReader>(sp => (ICatalogPricingReader)sp.GetRequiredService<IPricingService>());
        return services;
    }
}
