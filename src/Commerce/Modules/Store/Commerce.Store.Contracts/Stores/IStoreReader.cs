using Commerce.Framework.Core.Results;

namespace Commerce.Store.Contracts.Stores;

public interface IStoreReader
{
    Task<Result<StoreDetailDto>> GetByIdAsync(int storeId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoreSummaryDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
