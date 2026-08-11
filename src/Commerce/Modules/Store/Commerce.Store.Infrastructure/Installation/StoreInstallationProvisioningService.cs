using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Store.Application.Abstractions;
using Commerce.Store.Domain.Entities;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Store.Infrastructure.Installation;

public sealed class StoreInstallationProvisioningService(
    IStoreRepository storeRepository,
    ILanguageRepository languageRepository,
    IStoreCurrencyRepository currencyRepository) : IStoreInstallationProvisioningService
{
    public async Task<Result> CreateStoreAsync(
        StoreSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Url))
        {
            return Result.Failure(Error.Validation("Store name and URL are required."));
        }

        if (await storeRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false) is { Count: > 0 })
        {
            return Result.Success();
        }

        var defaultLanguageId = await EnsureDefaultLanguageAsync(cancellationToken).ConfigureAwait(false);
        var defaultCurrencyId = await EnsureDefaultCurrencyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var systemName = ToSystemName(request.Name);
            var store = StoreEntity.Create(
                systemName,
                request.Name.Trim(),
                request.Url.Trim(),
                defaultLanguageId,
                defaultCurrencyId,
                displayOrder: 0,
                isActive: true);

            await storeRepository.AddAsync(store, cancellationToken).ConfigureAwait(false);

            foreach (var host in ParseHosts(request.Hosts, request.Url))
            {
                store.AddDomain(
                    host,
                    request.Url.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http",
                    null,
                    isPrimary: true,
                    isSslRequired: request.Url.StartsWith("https", StringComparison.OrdinalIgnoreCase));
            }

            if (store.Domains.Count > 0)
            {
                await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> ConfigureLanguageAsync(
        LanguageSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Culture))
        {
            return Result.Failure(Error.Validation("Language name and culture are required."));
        }

        var languageCode = request.Culture.Split('-', StringSplitOptions.RemoveEmptyEntries)[0]
            .Trim()
            .ToLowerInvariant();

        var existingLanguage = await languageRepository
            .GetByLanguageCodeAsync(languageCode, cancellationToken)
            .ConfigureAwait(false);

        if (existingLanguage is not null)
        {
            if (request.IsDefault)
            {
                await UpdateStoresDefaultLanguageAsync(existingLanguage.Id, cancellationToken).ConfigureAwait(false);
            }

            return Result.Success();
        }

        try
        {
            var language = Language.Create(
                request.Name.Trim(),
                languageCode,
                request.Culture.Trim(),
                request.Name.Trim(),
                request.Rtl,
                displayOrder: 0,
                isActive: true);

            await languageRepository.AddAsync(language, cancellationToken).ConfigureAwait(false);

            if (request.IsDefault)
            {
                await UpdateStoresDefaultLanguageAsync(language.Id, cancellationToken).ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> ConfigureCurrencyAsync(
        CurrencySetupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            return Result.Failure(Error.Validation("Currency name and code are required."));
        }

        if (request.Rate < 0)
        {
            return Result.Failure(Error.Validation("Currency rate cannot be negative."));
        }

        var code = request.CurrencyCode.Trim().ToUpperInvariant();
        var existingCurrency = await currencyRepository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (existingCurrency is not null)
        {
            if (request.IsPrimary)
            {
                await UpdateStoresDefaultCurrencyAsync(existingCurrency.Id, cancellationToken).ConfigureAwait(false);
            }

            return Result.Success();
        }

        try
        {
            var currency = StoreCurrency.Create(
                code,
                request.Name.Trim(),
                code,
                request.Name.Trim(),
                request.Rate,
                decimalPlaces: code == "IRR" ? 0 : 2,
                displayOrder: 0,
                isActive: true);

            await currencyRepository.AddAsync(currency, cancellationToken).ConfigureAwait(false);

            if (request.IsPrimary)
            {
                await UpdateStoresDefaultCurrencyAsync(currency.Id, cancellationToken).ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<bool> HasStoreAsync(CancellationToken cancellationToken = default) =>
        (await storeRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false)).Count > 0;

    public async Task<bool> HasLanguageAsync(CancellationToken cancellationToken = default) =>
        (await languageRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false)).Count > 0;

    public async Task<bool> HasCurrencyAsync(CancellationToken cancellationToken = default) =>
        (await currencyRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false)).Count > 0;

    private async Task<int> EnsureDefaultLanguageAsync(CancellationToken cancellationToken)
    {
        var existing = (await languageRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false))
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefault();

        if (existing is not null)
        {
            return existing.Id;
        }

        var language = Language.Create("English", "en", "en-US", "English", isRtl: false);
        await languageRepository.AddAsync(language, cancellationToken).ConfigureAwait(false);
        return language.Id;
    }

    private async Task<int> EnsureDefaultCurrencyAsync(CancellationToken cancellationToken)
    {
        var existing = (await currencyRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false))
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefault();

        if (existing is not null)
        {
            return existing.Id;
        }

        var currency = StoreCurrency.Create("Iranian Rial", "IRR", "IRR", "Iranian Rial", rate: 1m, decimalPlaces: 0);
        await currencyRepository.AddAsync(currency, cancellationToken).ConfigureAwait(false);
        return currency.Id;
    }

    private async Task UpdateStoresDefaultLanguageAsync(int languageId, CancellationToken cancellationToken)
    {
        var stores = await storeRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false);
        foreach (var store in stores)
        {
            store.UpdateDetails(
                store.Name,
                store.Url,
                languageId,
                store.DefaultCurrencyId,
                store.DisplayOrder,
                store.IsActive);

            await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpdateStoresDefaultCurrencyAsync(int currencyId, CancellationToken cancellationToken)
    {
        var stores = await storeRepository.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false);
        foreach (var store in stores)
        {
            store.UpdateDetails(
                store.Name,
                store.Url,
                store.DefaultLanguageId,
                currencyId,
                store.DisplayOrder,
                store.IsActive);

            await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string ToSystemName(string name)
    {
        var normalized = new string(name
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_')
            .ToArray())
            .Replace(' ', '-');

        return string.IsNullOrWhiteSpace(normalized) ? "primary-store" : normalized;
    }

    private static IEnumerable<string> ParseHosts(string? hosts, string url)
    {
        if (!string.IsNullOrWhiteSpace(hosts))
        {
            foreach (var host in hosts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return host;
            }

            yield break;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            yield return uri.Host;
        }
    }
}
