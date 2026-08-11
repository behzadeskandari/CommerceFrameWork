using Commerce.Framework.Contracts.Seeding;
using Commerce.Store.Application.Abstractions;
using Commerce.Store.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Store.Infrastructure.Seeding;

public sealed class StoreIdentitySeeder : IModuleSeeder
{
    public int Order => 15;

    public string Name => "Store Identity";

    public string ModuleSystemName => "Commerce.Store";

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var scope = context.Services.CreateAsyncScope();
        var languageRepository = scope.ServiceProvider.GetRequiredService<ILanguageRepository>();
        var currencyRepository = scope.ServiceProvider.GetRequiredService<IStoreCurrencyRepository>();
        var storeRepository = scope.ServiceProvider.GetRequiredService<IStoreRepository>();

        var english = await EnsureLanguageAsync(
            languageRepository,
            "English",
            "en",
            "en-US",
            "English",
            isRtl: false,
            displayOrder: 0,
            cancellationToken).ConfigureAwait(false);

        await EnsureLanguageAsync(
            languageRepository,
            "Persian",
            "fa",
            "fa-IR",
            "فارسی",
            isRtl: true,
            displayOrder: 1,
            cancellationToken).ConfigureAwait(false);

        var irr = await EnsureCurrencyAsync(
            currencyRepository,
            "IRR",
            "Iranian Rial",
            "IRR",
            "Iranian Rial",
            rate: 1m,
            decimalPlaces: 0,
            displayOrder: 0,
            cancellationToken).ConfigureAwait(false);

        await EnsureCurrencyAsync(
            currencyRepository,
            "USD",
            "US Dollar",
            "$",
            "US Dollar",
            rate: 0.000024m,
            decimalPlaces: 2,
            displayOrder: 1,
            cancellationToken).ConfigureAwait(false);

        await EnsureCurrencyAsync(
            currencyRepository,
            "EUR",
            "Euro",
            "€",
            "Euro",
            rate: 0.000022m,
            decimalPlaces: 2,
            displayOrder: 2,
            cancellationToken).ConfigureAwait(false);

        if ((await storeRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false)).Count == 0)
        {
            var store = StoreEntity.Create(
                "primary-store",
                "Primary Store",
                "https://localhost:5100",
                english.Id,
                irr.Id,
                displayOrder: 0,
                isActive: true);

            await storeRepository.AddAsync(store, cancellationToken).ConfigureAwait(false);

            store.AddDomain("localhost", "https", 5100, isPrimary: true, isSslRequired: true);
            store.AddDomain("127.0.0.1", "https", 5100, isPrimary: false, isSslRequired: true);
            await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<Language> EnsureLanguageAsync(
        ILanguageRepository repository,
        string name,
        string languageCode,
        string cultureCode,
        string nativeName,
        bool isRtl,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetByLanguageCodeAsync(languageCode, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var language = Language.Create(name, languageCode, cultureCode, nativeName, isRtl, displayOrder);
        await repository.AddAsync(language, cancellationToken).ConfigureAwait(false);
        return language;
    }

    private static async Task<StoreCurrency> EnsureCurrencyAsync(
        IStoreCurrencyRepository repository,
        string code,
        string name,
        string symbol,
        string displayName,
        decimal rate,
        int decimalPlaces,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var currency = StoreCurrency.Create(code, name, symbol, displayName, rate, decimalPlaces, displayOrder);
        await repository.AddAsync(currency, cancellationToken).ConfigureAwait(false);
        return currency;
    }
}
