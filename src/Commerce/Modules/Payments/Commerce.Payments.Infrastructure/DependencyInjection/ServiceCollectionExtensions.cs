using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Security;
using Commerce.Payments.Application.Abstractions;
using Commerce.Payments.Application.DependencyInjection;
using Commerce.Payments.Infrastructure.Configuration;
using Commerce.Payments.Application.Abstractions;
using Commerce.Payments.Infrastructure.Persistence.Repositories;
using Commerce.Payments.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Payments.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, PaymentsPermissionContributor>();
        services.AddSingleton<ISettingDefinitionProvider, PaymentsSettingDefinitionProvider>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IGiftCardRepository, EfGiftCardRepository>();
        services.AddPaymentsApplication();
        return services;
    }
}
