using Commerce.Framework.Core.Results;
using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Inventory;

internal static class InventoryWarehouseAllocator
{
    public static async Task<(List<(InventoryItem Item, InventoryReservation Reservation)> Reserved, List<string> Errors)> ReserveAcrossWarehousesAsync(
        IInventoryRepository repository,
        int storeId,
        int offerId,
        int quantity,
        InventoryReferenceType referenceType,
        int referenceId,
        DateTime expiresAtUtc,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var items = await repository
            .ListByStoreAndOfferForUpdateAsync(storeId, offerId, cancellationToken)
            .ConfigureAwait(false);

        var tracked = items.Where(x => x.TrackInventory).ToList();
        if (tracked.Count == 0)
        {
            return ([], []);
        }

        var warehouses = await repository.ListWarehousesAsync(storeId, cancellationToken).ConfigureAwait(false);
        var defaultWarehouseId = warehouses.FirstOrDefault(x => x.IsDefault)?.Id;

        var ordered = tracked
            .OrderByDescending(x => x.WarehouseId == defaultWarehouseId)
            .ThenByDescending(x => x.GetAvailableAt(utcNow))
            .ToList();

        var reserved = new List<(InventoryItem, InventoryReservation)>();
        var remaining = quantity;
        var allowBackorder = tracked.Any(x => x.AllowBackorder);

        foreach (var item in ordered)
        {
            if (remaining <= 0)
            {
                break;
            }

            var available = item.GetAvailableAt(utcNow);
            var take = Math.Min(remaining, available);
            if (take <= 0)
            {
                continue;
            }

            var reservation = item.Reserve(take, referenceType, referenceId, expiresAtUtc, utcNow);
            reserved.Add((item, reservation));
            remaining -= take;
        }

        if (remaining > 0)
        {
            if (!allowBackorder)
            {
                return (reserved, [$"Insufficient inventory for offer '{offerId}'."]);
            }

            var backorderItem = ordered.FirstOrDefault(x => x.AllowBackorder) ?? ordered[^1];
            var reservation = backorderItem.Reserve(remaining, referenceType, referenceId, expiresAtUtc, utcNow);
            reserved.Add((backorderItem, reservation));
            remaining = 0;
        }

        return (reserved, []);
    }
}

public sealed class InventoryTransferService(
    IInventoryRepository inventoryRepository,
    ILogger<InventoryTransferService> logger) : IInventoryTransferService
{
    public async Task<Result<(InventoryMovementDto SourceMovement, InventoryMovementDto DestinationMovement)>> TransferAsync(
        TransferInventoryStockRequest request,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        var source = await inventoryRepository.GetByIdWithDetailsAsync(request.SourceInventoryItemId, cancellationToken).ConfigureAwait(false);
        var destination = await inventoryRepository.GetByIdWithDetailsAsync(request.DestinationInventoryItemId, cancellationToken).ConfigureAwait(false);

        if (source is null || destination is null)
        {
            return Result.Failure<(InventoryMovementDto, InventoryMovementDto)>(InventoryErrors.NotFound(request.SourceInventoryItemId));
        }

        if (source.StoreId != destination.StoreId || source.OfferId != destination.OfferId)
        {
            return Result.Failure<(InventoryMovementDto, InventoryMovementDto)>(InventoryErrors.InvalidTransfer("Transfers require the same store and offer."));
        }

        try
        {
            var transferId = Math.Max(source.Id, destination.Id);
            var sourceMovement = source.TransferOut(request.Quantity, transferId, request.Reason, actor);
            var destinationMovement = destination.TransferIn(request.Quantity, transferId, request.Reason, actor);
            await inventoryRepository.SaveAsync(source, cancellationToken).ConfigureAwait(false);
            await inventoryRepository.SaveAsync(destination, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Transferred {Quantity} units from inventory {SourceId} to {DestinationId}.",
                request.Quantity,
                source.Id,
                destination.Id);
            return Result.Success((InventoryMapper.ToMovement(sourceMovement), InventoryMapper.ToMovement(destinationMovement)));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Result.Failure<(InventoryMovementDto, InventoryMovementDto)>(InventoryErrors.InvalidTransfer(ex.Message));
        }
    }

    public async Task<Result<InventoryItemDetailDto>> ReceiveIncomingAsync(
        int inventoryItemId,
        ReceiveIncomingStockRequest request,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByIdWithDetailsAsync(inventoryItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.NotFound(inventoryItemId));
        }

        try
        {
            item.ReceiveIncoming(request.Quantity, request.Reason, InventoryReferenceType.Manual, null, actor);
            await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
            return Result.Success(InventoryMapper.ToDetail(item));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Result.Failure<InventoryItemDetailDto>(InventoryErrors.InvalidAdjustment(ex.Message));
        }
    }
}
