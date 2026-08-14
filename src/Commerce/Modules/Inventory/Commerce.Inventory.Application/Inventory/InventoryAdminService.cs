using Commerce.Catalog.Contracts.Offers;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Results;
using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Inventory;

public sealed class InventoryAdminService(
    IInventoryRepository inventoryRepository,
    IProductOfferReader offerReader,
    IStoreContext storeContext,
    ILogger<InventoryAdminService> logger) : IInventoryAdminService
{
    public async Task<Result<PagedInventorySummaryResult>> ListAsync(
        InventoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        var criteria = new InventoryListCriteria(
            Math.Max(1, query.Page),
            Math.Clamp(query.PageSize, 1, 100),
            query.StoreId ?? storeContext.CurrentStoreId,
            query.OfferId,
            query.ProductId,
            query.WarehouseId,
            query.AvailabilityStatus);

        var (items, total) = await inventoryRepository.ListAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedInventorySummaryResult(
            items.Select(InventoryMapper.ToSummary).ToList(),
            criteria.Page,
            criteria.PageSize,
            total));
    }

    public async Task<Result<InventoryItemDetailDto>> GetByIdAsync(
        int inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByIdWithDetailsAsync(inventoryItemId, cancellationToken).ConfigureAwait(false);
        return item is null
            ? Result.Failure<InventoryItemDetailDto>(InventoryErrors.NotFound(inventoryItemId))
            : Result.Success(InventoryMapper.ToDetail(item));
    }

    public async Task<Result<InventoryItemDetailDto>> CreateAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.StoreMismatch());
        }

        int? warehouseId = request.WarehouseId;
        if (!warehouseId.HasValue)
        {
            var defaultWarehouse = await inventoryRepository.GetDefaultWarehouseAsync(storeId.Value, cancellationToken).ConfigureAwait(false);
            warehouseId = defaultWarehouse?.Id;
        }

        var existing = warehouseId.HasValue
            ? await inventoryRepository.GetByStoreOfferAndWarehouseAsync(storeId.Value, request.OfferId, warehouseId, cancellationToken).ConfigureAwait(false)
            : await inventoryRepository.GetByStoreAndOfferAsync(storeId.Value, request.OfferId, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.AlreadyExists(storeId.Value, request.OfferId, warehouseId));
        }

        var offerResult = await offerReader.GetByIdAsync(request.OfferId, cancellationToken).ConfigureAwait(false);
        if (!offerResult.IsSuccess || offerResult.Value is null)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.OfferNotFound(request.OfferId));
        }

        var offer = offerResult.Value;
        if (offer.StoreId != storeId.Value)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.StoreMismatch());
        }

        var item = InventoryItem.Create(
            storeId.Value,
            offer.Id,
            offer.ProductId,
            offer.VariantId,
            request.TrackInventory,
            request.AllowBackorder,
            warehouseId,
            request.StockLocationId,
            request.LowStockThreshold);

        if (request.InitialOnHand > 0)
        {
            item.AdjustOnHand(
                request.InitialOnHand,
                InventoryMovementType.InitialStock,
                "Initial stock.",
                InventoryReferenceType.None,
                null,
                "admin");
        }

        if (request.InitialIncoming > 0)
        {
            item.AddIncoming(request.InitialIncoming, "Initial incoming stock.", "admin");
        }

        await inventoryRepository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Inventory item created for offer {OfferId} in store {StoreId}", offer.Id, storeId.Value);
        return Result.Success(InventoryMapper.ToDetail(item));
    }

    public async Task<Result<InventoryItemDetailDto>> AdjustAsync(
        int inventoryItemId,
        AdjustInventoryStockRequest request,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await inventoryRepository.GetByIdWithDetailsAsync(inventoryItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.NotFound(inventoryItemId));
        }

        try
        {
            item.AdjustOnHand(
                request.QuantityDelta,
                request.MovementType,
                request.Reason,
                InventoryReferenceType.Manual,
                null,
                actor);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.InvalidAdjustment(ex.Message));
        }

        await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Inventory item {InventoryItemId} adjusted by {Delta}", inventoryItemId, request.QuantityDelta);
        return Result.Success(InventoryMapper.ToDetail(item));
    }

    public async Task<Result<IReadOnlyList<InventoryMovementDto>>> ListMovementsAsync(
        int inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByIdWithDetailsAsync(inventoryItemId, cancellationToken).ConfigureAwait(false);
        return item is null
            ? Result.Failure<IReadOnlyList<InventoryMovementDto>>(InventoryErrors.NotFound(inventoryItemId))
            : Result.Success<IReadOnlyList<InventoryMovementDto>>(
                item.Movements.Select(InventoryMapper.ToMovement).ToList());
    }

    public async Task<Result<IReadOnlyList<InventoryReservationDto>>> ListReservationsAsync(
        int inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByIdWithDetailsAsync(inventoryItemId, cancellationToken).ConfigureAwait(false);
        return item is null
            ? Result.Failure<IReadOnlyList<InventoryReservationDto>>(InventoryErrors.NotFound(inventoryItemId))
            : Result.Success<IReadOnlyList<InventoryReservationDto>>(
                item.Reservations.Select(InventoryMapper.ToReservation).ToList());
    }

    public async Task<Result<InventoryItemDetailDto>> SetLowStockThresholdAsync(
        int inventoryItemId,
        SetLowStockThresholdRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByIdWithDetailsAsync(inventoryItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.NotFound(inventoryItemId));
        }

        try
        {
            item.SetLowStockThreshold(request.Threshold);
            await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
            return Result.Success(InventoryMapper.ToDetail(item));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.InvalidAdjustment(ex.Message));
        }
    }
}
