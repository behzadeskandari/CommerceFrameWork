using Commerce.Framework.Core.Results;
using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Inventory;

public sealed class InventoryReservationService(
    IInventoryRepository inventoryRepository,
    ILogger<InventoryReservationService> logger) : IInventoryReservationService
{
    public async Task<Result<InventoryReservationDto>> ReserveAsync(
        int inventoryItemId,
        int quantity,
        InventoryReferenceType referenceType,
        int referenceId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository
            .GetByIdWithDetailsAsync(inventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return Result.Failure<InventoryReservationDto>(InventoryErrors.ItemNotFound(inventoryItemId));
        }

        var utcNow = DateTime.UtcNow;
        if (!item.CanReserveAt(quantity, utcNow))
        {
            return Result.Failure<InventoryReservationDto>(InventoryErrors.InsufficientInventory(item.OfferId));
        }

        try
        {
            var reservation = item.Reserve(quantity, referenceType, referenceId, expiresAtUtc, utcNow);
            await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Reserved {Quantity} unit(s) on inventory item {InventoryItemId} for {ReferenceType} {ReferenceId}.",
                quantity,
                inventoryItemId,
                referenceType,
                referenceId);

            return Result.Success(InventoryMapper.ToReservation(reservation));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<InventoryReservationDto>(InventoryErrors.InsufficientInventory(item.OfferId, ex.Message));
        }
    }

    public async Task<Result> ReleaseAsync(
        int reservationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var reservation = await inventoryRepository
            .GetReservationByIdAsync(reservationId, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound(reservationId));
        }

        var item = await inventoryRepository
            .GetByIdWithDetailsAsync(reservation.InventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return Result.Failure(InventoryErrors.ItemNotFound(reservation.InventoryItemId));
        }

        item.ReleaseReservation(reservation, reason, DateTime.UtcNow);
        await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Released inventory reservation {ReservationId}.", reservationId);
        return Result.Success();
    }

    public async Task<Result> ConvertAsync(
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await inventoryRepository
            .GetReservationByIdAsync(reservationId, cancellationToken)
            .ConfigureAwait(false);

        if (reservation is null)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound(reservationId));
        }

        var item = await inventoryRepository
            .GetByIdWithDetailsAsync(reservation.InventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return Result.Failure(InventoryErrors.ItemNotFound(reservation.InventoryItemId));
        }

        if (!reservation.IsActive(DateTime.UtcNow))
        {
            return Result.Failure(InventoryErrors.InvalidReservationState(reservationId));
        }

        reservation.Convert(DateTime.UtcNow);
        await inventoryRepository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Converted inventory reservation {ReservationId}.", reservationId);
        return Result.Success();
    }
}
