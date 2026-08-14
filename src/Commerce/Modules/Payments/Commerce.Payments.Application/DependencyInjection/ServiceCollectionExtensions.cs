using Commerce.Checkout.Contracts.Checkout;
using Commerce.Payments.Application.GiftCards;
using Commerce.Payments.Application.Payments;
using Commerce.Payments.Contracts.Admin;
using Commerce.Payments.Contracts.Callbacks;
using Commerce.Payments.Contracts.GiftCards;
using Commerce.Payments.Contracts.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Payments.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentAdminService, PaymentAdminService>();
        services.AddScoped<IOrderPaymentSyncService, OrderPaymentSyncService>();
        services.AddScoped<PaymentProviderResolver>();
        services.AddScoped<IPaymentMethodProvider, PaymentCheckoutMethodProvider>();
        services.AddScoped<IPaymentProviderSettingsReader, PaymentProviderSettingsReader>();
        services.AddScoped<PaymentCallbackDispatcher>();
        services.AddScoped<IPaymentCallbackDispatcher>(sp => sp.GetRequiredService<PaymentCallbackDispatcher>());
        services.AddScoped<IGiftCardAdminService, GiftCardAdminService>();
        services.AddScoped<IGiftCardValidationService, GiftCardValidationService>();
        services.AddScoped<IGiftCardRedemptionService, GiftCardRedemptionService>();
        services.AddScoped<IGiftCardReader, GiftCardReader>();
        return services;
    }
}
