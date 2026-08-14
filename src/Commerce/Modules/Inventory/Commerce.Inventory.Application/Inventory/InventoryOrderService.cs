using Commerce.Framework.Core.Results;
using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Inventory;

public sealed class InventoryOrderService(
    IInventoryRepository inventoryRepository,
    InventorySettings settings,
    ILogger<InventoryOrderService> logger) : IInventoryOrderService
{
    public async Task<InventoryOrderReservationResult> ReserveForOrderAsync(
        InventoryOrderReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        var utcNow = DateTime.UtcNow;
        var expiresAt = utcNow.Add(settings.DefaultReservationDuration);
        var reservedItems = new List<(Domain.Entities.InventoryItem Item, Domain.Entities.InventoryReservation Reservation)>();

        foreach (var line in request.Lines)
        {
            var lineResult = await InventoryWarehouseAllocator.ReserveAcrossWarehousesAsync(
                inventoryRepository,
                request.StoreId,
                line.OfferId,
                line.Quantity,
                InventoryReferenceType.Order,
                request.OrderId,
                expiresAt,
                utcNow,
                cancellationToken).ConfigureAwait(false);

            if (lineResult.Errors.Count > 0)
            {
                errors.AddRange(lineResult.Errors);
            }
            else
            {
                reservedItems.AddRange(lineResult.Reserved);
            }
        }

        if (errors.Count > 0)
        {
            foreach (var (item, reservation) in reservedItems)
            {
                item.ReleaseReservation(reservation, "Reservation rolled back.", utcNow);
                await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
            }

            return new InventoryOrderReservationResult(false, errors);
        }

        foreach (var (item, _) in reservedItems)
        {
            await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
        }

        if (reservedItems.Count > 0)
        {
            logger.LogInformation(
                "Reserved inventory for order {OrderId} across {Count} item(s).",
                request.OrderId,
                reservedItems.Count);
        }

        return new InventoryOrderReservationResult(true, []);
    }

    public async Task<Result> ReleaseForOrderAsync(
        int orderId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var reservations = await inventoryRepository
            .GetActiveReservationsForReferenceAsync(InventoryReferenceType.Order, orderId, cancellationToken)
            .ConfigureAwait(false);

        if (reservations.Count == 0)
        {
            return Result.Success();
        }

        var utcNow = DateTime.UtcNow;
        var touchedItems = new HashSet<int>();

        foreach (var reservation in reservations)
        {
            var item = await inventoryRepository
                .GetByIdWithDetailsAsync(reservation.InventoryItemId, cancellationToken)
                .ConfigureAwait(false);

            if (item is null || item.StoreId != storeId)
            {
                continue;
            }

            item.ReleaseReservation(reservation, "Order cancelled.", utcNow);
            await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
            touchedItems.Add(item.Id);
        }

        logger.LogInformation(
            "Released inventory reservations for cancelled order {OrderId} on {Count} item(s).",
            orderId,
            touchedItems.Count);

        return Result.Success();
    }

    public async Task<Result> ConvertForOrderAsync(
        int orderId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var reservations = await inventoryRepository
            .GetActiveReservationsForReferenceAsync(InventoryReferenceType.Order, orderId, cancellationToken)
            .ConfigureAwait(false);

        if (reservations.Count == 0)
        {
            return Result.Success();
        }

        var utcNow = DateTime.UtcNow;
        foreach (var reservation in reservations)
        {
            var item = await inventoryRepository
                .GetByIdWithDetailsAsync(reservation.InventoryItemId, cancellationToken)
                .ConfigureAwait(false);

            if (item is null || item.StoreId != storeId)
            {
                continue;
            }

            item.ConvertReservationToSale(reservation, utcNow, "order-paid");
            await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Converted inventory reservations to sale for order {OrderId}.", orderId);
        return Result.Success();
    }

    public async Task<Result> ReleasePartialForOrderAsync(
        int orderId,
        int storeId,
        IReadOnlyList<InventoryOrderLineAdjustment> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            return Result.Success();
        }

        var reservations = await inventoryRepository
            .GetActiveReservationsForReferenceAsync(InventoryReferenceType.Order, orderId, cancellationToken)
            .ConfigureAwait(false);

        var utcNow = DateTime.UtcNow;
        foreach (var line in lines)
        {
            var remaining = line.Quantity;
            foreach (var reservation in reservations.Where(x => x.IsActive(utcNow)))
            {
                if (remaining <= 0)
                {
                    break;
                }

                var item = await inventoryRepository
                    .GetByIdWithDetailsAsync(reservation.InventoryItemId, cancellationToken)
                    .ConfigureAwait(false);

                if (item is null || item.StoreId != storeId || item.OfferId != line.OfferId)
                {
                    continue;
                }

                var releaseQty = Math.Min(remaining, reservation.Quantity);
                if (releaseQty == reservation.Quantity)
                {
                    item.ReleaseReservation(reservation, "Partial order cancellation.", utcNow);
                }
                else
                {
                    reservation.ReduceQuantity(releaseQty, utcNow);
                    item.ReleaseReservedQuantity(releaseQty, utcNow);
                }

                await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
                remaining -= releaseQty;
            }

            if (remaining > 0)
            {
                logger.LogWarning(
                    "Could not release all reserved quantity for offer {OfferId} on order {OrderId}. Remaining: {Remaining}.",
                    line.OfferId,
                    orderId,
                    remaining);
            }
        }

        return Result.Success();
    }

    public async Task<Result> RestockForOrderAsync(
        int orderId,
        int storeId,
        IReadOnlyList<InventoryOrderLineAdjustment> lines,
        string reason,
        CancellationToken cancellationToken = default)
    {
        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                continue;
            }

            var items = await inventoryRepository
                .ListByStoreAndOfferForUpdateAsync(storeId, line.OfferId, cancellationToken)
                .ConfigureAwait(false);

            var target = items.FirstOrDefault();
            if (target is null)
            {
                logger.LogWarning(
                    "No inventory item found to restock offer {OfferId} for order {OrderId}.",
                    line.OfferId,
                    orderId);
                continue;
            }

            target.AdjustOnHand(
                line.Quantity,
                InventoryMovementType.Return,
                reason,
                InventoryReferenceType.Order,
                orderId,
                "order-lifecycle");

            await inventoryRepository.SaveAsync(target, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Restocked inventory for order {OrderId}.", orderId);
        return Result.Success();
    }
}

public sealed class InventoryReservationExpirationService(
    IInventoryRepository inventoryRepository,
    ILogger<InventoryReservationExpirationService> logger) : IInventoryReservationExpirationService
{
    public async Task<int> ExpireReservationsAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var expired = await inventoryRepository
            .GetExpiredActiveReservationsAsync(utcNow, cancellationToken)
            .ConfigureAwait(false);

        var count = 0;
        foreach (var reservation in expired)
        {
            var item = await inventoryRepository
                .GetByIdWithDetailsAsync(reservation.InventoryItemId, cancellationToken)
                .ConfigureAwait(false);

            if (item is null || !reservation.IsActive(utcNow))
            {
                continue;
            }

            reservation.MarkExpired(utcNow);
            item.ReleaseReservation(reservation, "Reservation expired.", utcNow);
            await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
            count++;
        }

        if (count > 0)
        {
            logger.LogInformation("Expired {Count} inventory reservation(s).", count);
        }

        return count;
    }
}
