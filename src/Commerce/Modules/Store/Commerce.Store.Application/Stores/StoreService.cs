using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Store.Application.Abstractions;
using Commerce.Store.Contracts.Stores;
using Commerce.Store.Domain.Entities;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Store.Application.Stores;

public sealed class StoreService(IStoreRepository storeRepository) : IStoreService
{
    public async Task<Result<StoreDetailDto>> CreateAsync(
        CreateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (await storeRepository.GetBySystemNameAsync(request.SystemName, cancellationToken)
                    .ConfigureAwait(false) is not null)
            {
                return Result.Failure<StoreDetailDto>(
                    Error.Conflict($"A store with system name '{request.SystemName}' already exists."));
            }

            var store = StoreEntity.Create(
                request.SystemName,
                request.Name,
                request.Url,
                request.DefaultLanguageId,
                request.DefaultCurrencyId,
                request.DisplayOrder,
                request.IsActive);

            await storeRepository.AddAsync(store, cancellationToken).ConfigureAwait(false);

            if (request.Domains is { Count: > 0 })
            {
                foreach (var domainRequest in request.Domains)
                {
                    store.AddDomain(
                        domainRequest.Host,
                        domainRequest.Scheme,
                        domainRequest.Port,
                        domainRequest.IsPrimary,
                        domainRequest.IsSslRequired);
                }

                await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
            }

            return Result.Success(MapDetail(store));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<StoreDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<StoreDetailDto>> UpdateAsync(
        int storeId,
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var store = await storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store is null || store.IsDeleted)
        {
            return Result.Failure<StoreDetailDto>(Error.NotFound($"Store '{storeId}' was not found."));
        }

        try
        {
            store.UpdateDetails(
                request.Name,
                request.Url,
                request.DefaultLanguageId,
                request.DefaultCurrencyId,
                request.DisplayOrder,
                request.IsActive);

            await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(store));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<StoreDetailDto>(Error.Validation(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<StoreDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store is null || store.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Store '{storeId}' was not found."));
        }

        store.MarkDeleted();
        await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<StoreDetailDto>> AddDomainAsync(
        int storeId,
        AddStoreDomainRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var store = await storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store is null || store.IsDeleted)
        {
            return Result.Failure<StoreDetailDto>(Error.NotFound($"Store '{storeId}' was not found."));
        }

        try
        {
            store.AddDomain(
                request.Host,
                request.Scheme,
                request.Port,
                request.IsPrimary,
                request.IsSslRequired);

            await storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(store));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<StoreDetailDto>(Error.Validation(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<StoreDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<StoreDetailDto>> GetByIdAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store is null || store.IsDeleted)
        {
            return Result.Failure<StoreDetailDto>(Error.NotFound($"Store '{storeId}' was not found."));
        }

        return Result.Success(MapDetail(store));
    }

    public async Task<Result<IReadOnlyList<StoreSummaryDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var stores = await storeRepository.ListAsync(includeInactive, cancellationToken).ConfigureAwait(false);
        var summaries = stores
            .Select(MapSummary)
            .ToList();

        return Result.Success<IReadOnlyList<StoreSummaryDto>>(summaries);
    }

    internal static StoreSummaryDto MapSummary(StoreEntity store) =>
        new(
            store.Id,
            store.SystemName,
            store.Name,
            store.Url,
            store.IsActive,
            store.DisplayOrder,
            store.DefaultLanguageId,
            store.DefaultCurrencyId,
            store.CreatedAtUtc);

    internal static StoreDetailDto MapDetail(StoreEntity store) =>
        new(
            store.Id,
            store.SystemName,
            store.Name,
            store.Url,
            store.IsActive,
            store.DisplayOrder,
            store.DefaultLanguageId,
            store.DefaultCurrencyId,
            store.CreatedAtUtc,
            store.UpdatedAtUtc,
            store.Domains.Select(MapDomain).ToList());

    internal static StoreDomainDto MapDomain(StoreDomain domain) =>
        new(
            domain.Id,
            domain.StoreId,
            domain.Host,
            domain.Scheme,
            domain.Port,
            domain.IsPrimary,
            domain.IsSslRequired);
}
