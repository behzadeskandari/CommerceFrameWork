using Commerce.Store.Application.Currencies;
using Commerce.Store.Application.Languages;
using Commerce.Store.Application.Stores;
using Commerce.Store.Contracts.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Store.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStoreApplication(this IServiceCollection services)
    {
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IStoreReader>(sp => sp.GetRequiredService<IStoreService>());

        return services;
    }
}
