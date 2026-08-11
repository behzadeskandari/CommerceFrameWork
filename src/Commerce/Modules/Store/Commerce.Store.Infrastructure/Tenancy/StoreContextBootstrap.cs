using Commerce.Framework.Contracts.Localization;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Store.Application.Abstractions;

namespace Commerce.Store.Infrastructure.Tenancy;

public sealed class StoreContextBootstrap(
    IStoreResolver storeResolver,
    ILanguageResolver languageResolver,
    IStoreContextAccessor storeContextAccessor,
    IStoreCurrencyRepository currencyRepository) : IStoreContextBootstrap
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var resolution = await storeResolver
            .ResolveAsync("localhost", null, "https", cancellationToken)
            .ConfigureAwait(false);

        if (resolution is null)
        {
            return;
        }

        storeContextAccessor.SetStore(resolution.StoreId, resolution.SystemName, resolution.Name);

        var language = await languageResolver
            .ResolveAsync(
                resolution.StoreId,
                resolution.DefaultLanguageId,
                acceptLanguageHeader: null,
                preferenceCookie: null,
                cancellationToken)
            .ConfigureAwait(false);

        if (language is not null)
        {
            storeContextAccessor.SetLanguage(
                language.LanguageId,
                language.LanguageCode,
                language.CultureCode,
                language.IsRtl);
        }

        var currency = await currencyRepository
            .GetByIdAsync(resolution.DefaultCurrencyId, cancellationToken)
            .ConfigureAwait(false);

        if (currency is not null && currency.IsActive)
        {
            storeContextAccessor.SetCurrency(currency.Id, currency.Code);
        }
    }
}
