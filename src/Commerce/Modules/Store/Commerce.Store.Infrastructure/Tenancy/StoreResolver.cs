using Commerce.Framework.Contracts.Tenancy;
using Commerce.Store.Application.Abstractions;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Store.Infrastructure.Tenancy;

public sealed class StoreResolver(IStoreRepository storeRepository) : IStoreResolver
{
    public async Task<StoreResolutionResult?> ResolveAsync(
        string host,
        int? port,
        string scheme,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        StoreEntity? store = null;
        var normalizedHost = host.Trim().ToLowerInvariant();

        if (!IsLocalhost(normalizedHost))
        {
            store = await storeRepository.FindByHostAsync(normalizedHost, port, cancellationToken)
                .ConfigureAwait(false);
        }

        store ??= await storeRepository.GetDefaultActiveAsync(cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        return new StoreResolutionResult(
            store.Id,
            store.SystemName,
            store.Name,
            store.DefaultLanguageId,
            store.DefaultCurrencyId);
    }

    private static bool IsLocalhost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
