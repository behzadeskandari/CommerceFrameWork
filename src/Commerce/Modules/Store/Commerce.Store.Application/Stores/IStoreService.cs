using Commerce.Framework.Core.Results;
using Commerce.Store.Application.Stores;
using Commerce.Store.Contracts.Stores;

namespace Commerce.Store.Application.Stores;

public interface IStoreService : IStoreReader
{
    Task<Result<StoreDetailDto>> CreateAsync(
        CreateStoreRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<StoreDetailDto>> UpdateAsync(
        int storeId,
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int storeId, CancellationToken cancellationToken = default);

    Task<Result<StoreDetailDto>> AddDomainAsync(
        int storeId,
        AddStoreDomainRequest request,
        CancellationToken cancellationToken = default);
}
