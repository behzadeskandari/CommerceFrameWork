using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Application.Addresses;
using Commerce.Customers.Application.Affiliates;
using Commerce.Customers.Application.Authentication;
using Commerce.Customers.Application.CustomerAccount;
using Commerce.Customers.Application.Customers;
using Commerce.Customers.Contracts.Affiliates;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Contracts.Customers;
using Commerce.Orders.Contracts.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Customers.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomersApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICustomerReader>(sp => sp.GetRequiredService<ICustomerService>());
        services.AddScoped<ICustomerAddressReader>(sp => sp.GetRequiredService<ICustomerAddressService>());

        services.AddScoped<ICustomerPreferenceService, CustomerPreferenceService>();
        services.AddScoped<ICustomerSegmentAdminService, CustomerSegmentAdminService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<ILoyaltyRewardAdminService, LoyaltyRewardAdminService>();
        services.AddScoped<IStoreCreditService, StoreCreditService>();
        services.AddScoped<IStoreCreditReader>(sp => sp.GetRequiredService<StoreCreditService>());
        services.AddScoped<ICustomerActivityService, CustomerActivityService>();
        services.AddScoped<ICustomerAccountAdminService, CustomerAccountAdminService>();
        services.AddScoped<ICustomerAccountStorefrontService, CustomerAccountStorefrontService>();
        services.AddScoped<IAffiliateAdminService, AffiliateAdminService>();
        services.AddScoped<IAffiliateReader>(sp => sp.GetRequiredService<AffiliateAdminService>());
        services.AddScoped<IAffiliateValidationService, AffiliateValidationService>();
        services.AddScoped<IAffiliateReferralService, AffiliateReferralService>();
        services.AddScoped<IAffiliateCommissionService, AffiliateCommissionService>();
        services.AddScoped<IOrderPaidHandler, OrderPaidLoyaltyHandler>();

        return services;
    }
}
