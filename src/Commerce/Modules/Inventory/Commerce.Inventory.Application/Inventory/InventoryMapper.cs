using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;

namespace Commerce.Inventory.Application.Inventory;

internal static class InventoryMapper
{
    public static InventoryItemSummaryDto ToSummary(InventoryItem item) =>
        new(
            item.Id,
            item.StoreId,
            item.OfferId,
            item.ProductId,
            item.VariantId,
            item.TrackInventory,
            item.AllowBackorder,
            item.OnHand,
            item.Reserved,
            item.Available,
            item.GetAvailabilityStatus(),
            item.UpdatedAtUtc);

    public static InventoryItemDetailDto ToDetail(InventoryItem item) =>
        new(
            item.Id,
            item.StoreId,
            item.OfferId,
            item.ProductId,
            item.VariantId,
            item.WarehouseId,
            item.TrackInventory,
            item.AllowBackorder,
            item.OnHand,
            item.Reserved,
            item.Available,
            item.GetAvailabilityStatus(),
            item.CreatedAtUtc,
            item.UpdatedAtUtc);

    public static OfferAvailabilityDto ToAvailability(InventoryItem item, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var activeReserved = item.GetActiveReservedQuantity(now);
        var available = item.GetAvailableAt(now);
        var status = item.GetAvailabilityStatusAt(now);

        return new(
            item.Id,
            item.StoreId,
            item.OfferId,
            item.ProductId,
            item.VariantId,
            item.TrackInventory,
            item.AllowBackorder,
            item.OnHand,
            activeReserved,
            available,
            status,
            CanPurchaseAt(item, 1, now),
            status == InventoryAvailabilityStatus.Backorder);
    }

    public static OfferAvailabilityDto NotTracked(int storeId, int offerId, int productId, int? variantId) =>
        new(
            0,
            storeId,
            offerId,
            productId,
            variantId,
            false,
            false,
            0,
            0,
            int.MaxValue,
            InventoryAvailabilityStatus.NotTracked,
            true,
            false);

    public static InventoryMovementDto ToMovement(Domain.Entities.InventoryMovement movement) =>
        new(
            movement.Id,
            movement.InventoryItemId,
            movement.QuantityDelta,
            movement.MovementType,
            movement.Reason,
            movement.ReferenceType,
            movement.ReferenceId,
            movement.CreatedBy,
            movement.CreatedAtUtc);

    public static InventoryReservationDto ToReservation(Domain.Entities.InventoryReservation reservation) =>
        new(
            reservation.Id,
            reservation.InventoryItemId,
            reservation.Quantity,
            reservation.ReferenceType,
            reservation.ReferenceId,
            reservation.Status,
            reservation.ExpiresAtUtc,
            reservation.ReleaseReason,
            reservation.CreatedAtUtc,
            reservation.UpdatedAtUtc);

    public static bool CanPurchase(InventoryItem item, int quantity) =>
        CanPurchaseAt(item, quantity, DateTime.UtcNow);

    public static bool CanPurchaseAt(InventoryItem item, int quantity, DateTime utcNow) =>
        !item.TrackInventory || item.CanReserveAt(quantity, utcNow);
}
