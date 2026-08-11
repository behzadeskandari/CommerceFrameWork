using Commerce.Framework.Contracts.Tenancy;

namespace Commerce.Framework.Contracts.Tenancy;

public interface IStoreResolver
{
    Task<StoreResolutionResult?> ResolveAsync(
        string host,
        int? port,
        string scheme,
        CancellationToken cancellationToken = default);
}

public sealed record StoreResolutionResult(
    int StoreId,
    string SystemName,
    string Name,
    int DefaultLanguageId,
    int DefaultCurrencyId);
