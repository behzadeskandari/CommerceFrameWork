using Commerce.Pricing.Application.DependencyInjection;
using Commerce.Pricing.Infrastructure.Migrations;
using Commerce.Pricing.Infrastructure.Persistence;
using Commerce.Pricing.Infrastructure.Persistence.Repositories;
using Commerce.Pricing.Infrastructure.Security;
using Commerce.Pricing.Application.Abstractions;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Pricing.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPricingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, PricingPermissionContributor>();
        services.AddScoped<IPricingRepository, EfPricingRepository>();
        services.AddPricingApplication();
        return services;
    }
}
