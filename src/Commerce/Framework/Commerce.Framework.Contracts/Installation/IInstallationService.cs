using Commerce.Framework.Core.Results;

namespace Commerce.Framework.Contracts.Installation;

public interface IInstallationService
{
    Task<Result<IReadOnlyList<RequirementCheckResult>>> ValidateRequirementsAsync(
        CancellationToken cancellationToken = default);

    Task<Result> ConfigureDatabaseAsync(
        DatabaseSetupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<int>> RunMigrationsAsync(CancellationToken cancellationToken = default);

    Task<Result> RunSeedAsync(CancellationToken cancellationToken = default);

    Task<Result> CreateAdministratorAsync(
        AdministratorSetupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> CreateStoreAsync(
        StoreSetupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ConfigureLanguageAsync(
        LanguageSetupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ConfigureCurrencyAsync(
        CurrencySetupRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteInstallationAsync(CancellationToken cancellationToken = default);
}

public sealed record DatabaseSetupRequest(
    string Provider,
    string ConnectionString);

public sealed record AdministratorSetupRequest(
    string Email,
    string Username,
    string Password);

public sealed record StoreSetupRequest(
    string Name,
    string Url,
    string? Hosts);

public sealed record LanguageSetupRequest(
    string Name,
    string Culture,
    bool Rtl,
    bool IsDefault);

public sealed record CurrencySetupRequest(
    string Name,
    string CurrencyCode,
    decimal Rate,
    bool IsPrimary);
