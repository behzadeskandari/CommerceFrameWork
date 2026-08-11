using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Currency;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Localization;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Store.Application.Abstractions;
using Commerce.Store.Application.DependencyInjection;
using Commerce.Store.Infrastructure.Configuration;
using Commerce.Store.Infrastructure.Exchange;
using Commerce.Store.Infrastructure.Installation;
using Commerce.Store.Infrastructure.Localization;
using Commerce.Store.Infrastructure.Persistence.Repositories;
using Commerce.Store.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Store.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStoreInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IStoreRepository, EfStoreRepository>();
        services.AddScoped<ILanguageRepository, EfLanguageRepository>();
        services.AddScoped<IStoreCurrencyRepository, EfStoreCurrencyRepository>();

        services.AddScoped<IStoreResolver, StoreResolver>();
        services.AddScoped<ILanguageResolver, LanguageResolver>();
        services.AddScoped<ICurrencyExchangeRateProvider, FixedExchangeRateProvider>();
        services.AddScoped<ICurrencyConverter, CurrencyConverter>();

        services.AddSingleton<ISettingDefinitionProvider, StoreSettingDefinitionProvider>();
        services.AddScoped<ISettingService, SettingService>();

        services.AddScoped<IStoreInstallationProvisioningService, StoreInstallationProvisioningService>();

        services.AddScoped<IStoreContextBootstrap, StoreContextBootstrap>();

        return services;
    }
}
