using Commerce.Framework.Core.Results;

namespace Commerce.Framework.Contracts.Installation;

public interface IStoreInstallationProvisioningService
{
    Task<Result> CreateStoreAsync(
        StoreSetupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ConfigureLanguageAsync(
        LanguageSetupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ConfigureCurrencyAsync(
        CurrencySetupRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> HasStoreAsync(CancellationToken cancellationToken = default);

    Task<bool> HasLanguageAsync(CancellationToken cancellationToken = default);

    Task<bool> HasCurrencyAsync(CancellationToken cancellationToken = default);
}
