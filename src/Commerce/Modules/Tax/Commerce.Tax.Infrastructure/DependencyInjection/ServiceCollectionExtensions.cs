using Commerce.Framework.Contracts.Configuration;

using Commerce.Framework.Contracts.Security;

using Commerce.Tax.Application.Abstractions;

using Commerce.Tax.Application.DependencyInjection;

using Commerce.Tax.Infrastructure.Configuration;

using Commerce.Tax.Infrastructure.Persistence.Repositories;

using Commerce.Tax.Infrastructure.Security;

using Microsoft.Extensions.DependencyInjection;



namespace Commerce.Tax.Infrastructure.DependencyInjection;



public static class ServiceCollectionExtensions

{

    public static IServiceCollection AddTaxInfrastructure(this IServiceCollection services)

    {

        services.AddSingleton<IModulePermissionContributor, TaxPermissionContributor>();

        services.AddSingleton<ISettingDefinitionProvider, TaxSettingDefinitionProvider>();

        services.AddScoped<ITaxRepository, EfTaxRepository>();

        services.AddTaxApplication();

        return services;

    }

}


