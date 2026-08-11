using Commerce.Customers.Application.Addresses;
using Commerce.Customers.Application.Authentication;
using Commerce.Customers.Application.Customers;
using Commerce.Customers.Contracts.Customers;
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

        return services;
    }
}
