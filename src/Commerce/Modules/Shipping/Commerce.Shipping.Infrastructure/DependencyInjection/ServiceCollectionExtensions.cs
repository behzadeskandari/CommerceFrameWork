using Commerce.Shipping.Application.DependencyInjection;
using Commerce.Shipping.Infrastructure.Configuration;
using Commerce.Shipping.Infrastructure.Migrations;
using Commerce.Shipping.Infrastructure.Persistence;
using Commerce.Shipping.Infrastructure.Persistence.Repositories;
using Commerce.Shipping.Infrastructure.Security;
using Commerce.Shipping.Application.Abstractions;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Shipping.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShippingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, ShippingPermissionContributor>();
        services.AddSingleton<ISettingDefinitionProvider, ShippingSettingDefinitionProvider>();
        services.AddScoped<IShippingRepository, EfShippingRepository>();
        services.AddShippingApplication();
        return services;
    }
}
