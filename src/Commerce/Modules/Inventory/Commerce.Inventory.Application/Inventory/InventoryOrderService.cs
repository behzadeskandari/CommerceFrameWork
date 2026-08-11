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
            var item = await inventoryRepository
                .GetByStoreAndOfferForUpdateAsync(request.StoreId, line.OfferId, cancellationToken)
                .ConfigureAwait(false);

            if (item is null || !item.TrackInventory)
            {
                continue;
            }

            if (!item.CanReserveAt(line.Quantity, utcNow))
            {
                errors.Add($"Insufficient inventory for offer '{line.OfferId}'.");
                continue;
            }

            try
            {
                var reservation = item.Reserve(
                    line.Quantity,
                    InventoryReferenceType.Order,
                    request.OrderId,
                    expiresAt,
                    utcNow);
                reservedItems.Add((item, reservation));
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
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
