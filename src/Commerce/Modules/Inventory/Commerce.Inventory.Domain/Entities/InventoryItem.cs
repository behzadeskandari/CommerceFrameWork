using Commerce.Inventory.Domain.Enums;
using Commerce.Inventory.Domain.Events;
using Commerce.Framework.Core.Entities;

namespace Commerce.Inventory.Domain.Entities;

public sealed class InventoryItem : AggregateRoot
{
    private readonly List<InventoryMovement> _movements = [];
    private readonly List<InventoryReservation> _reservations = [];

    private InventoryItem()
    {
    }

    public int StoreId { get; private set; }

    public int OfferId { get; private set; }

    public int ProductId { get; private set; }

    public int? VariantId { get; private set; }

    public int? WarehouseId { get; private set; }

    public bool TrackInventory { get; private set; }

    public bool AllowBackorder { get; private set; }

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<InventoryMovement> Movements => _movements;

    public IReadOnlyCollection<InventoryReservation> Reservations => _reservations;

    public int Available => Math.Max(0, OnHand - Reserved);

    public int GetActiveReservedQuantity(DateTime utcNow) =>
        _reservations.Count > 0
            ? _reservations.Where(r => r.IsActive(utcNow)).Sum(r => r.Quantity)
            : Reserved;

    public int GetAvailableAt(DateTime utcNow) =>
        Math.Max(0, OnHand - GetActiveReservedQuantity(utcNow));

    public static InventoryItem Create(
        int storeId,
        int offerId,
        int productId,
        int? variantId,
        bool trackInventory,
        bool allowBackorder,
        int? warehouseId = null)
    {
        ValidateStore(storeId);
        ValidateOffer(offerId);
        ValidateProduct(productId);

        var utcNow = DateTime.UtcNow;
        return new InventoryItem
        {
            StoreId = storeId,
            OfferId = offerId,
            ProductId = productId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            TrackInventory = trackInventory,
            AllowBackorder = allowBackorder,
            OnHand = 0,
            Reserved = 0,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public InventoryMovement AdjustOnHand(
        int quantityDelta,
        InventoryMovementType movementType,
        string reason,
        InventoryReferenceType referenceType,
        int? referenceId,
        string? createdBy)
    {
        if (!TrackInventory && quantityDelta != 0)
        {
            throw new InvalidOperationException("Cannot adjust stock for non-tracked inventory item.");
        }

        if (OnHand + quantityDelta < 0)
        {
            throw new InvalidOperationException("On-hand quantity cannot become negative.");
        }

        OnHand += quantityDelta;
        var movement = InventoryMovement.Create(
            Id,
            quantityDelta,
            movementType,
            reason,
            referenceType,
            referenceId,
            createdBy);
        _movements.Add(movement);
        Touch();
        RaiseDomainEvent(new InventoryAdjustedEvent(Id, StoreId, OfferId, quantityDelta, movementType.ToString()));
        return movement;
    }

    public InventoryReservation Reserve(
        int quantity,
        InventoryReferenceType referenceType,
        int referenceId,
        DateTime expiresAtUtc,
        DateTime utcNow)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (!TrackInventory)
        {
            throw new InvalidOperationException("Non-tracked inventory items do not require reservations.");
        }

        if (!CanReserveAt(quantity, utcNow))
        {
            throw new InvalidOperationException("Insufficient inventory.");
        }

        Reserved += quantity;
        var reservation = InventoryReservation.Create(
            Id,
            quantity,
            referenceType,
            referenceId,
            expiresAtUtc,
            utcNow);
        _reservations.Add(reservation);
        Touch();
        RaiseDomainEvent(new InventoryReservedEvent(
            Id,
            0,
            StoreId,
            OfferId,
            quantity,
            referenceType.ToString(),
            referenceId));
        return reservation;
    }

    public void ReleaseReservation(InventoryReservation reservation, string reason, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        if (reservation.InventoryItemId != Id)
        {
            throw new InvalidOperationException("Reservation does not belong to this inventory item.");
        }

        if (!reservation.IsActive(utcNow))
        {
            return;
        }

        reservation.Release(reason, utcNow);
        Reserved = Math.Max(0, Reserved - reservation.Quantity);
        Touch();
        RaiseDomainEvent(new InventoryReservationReleasedEvent(
            Id,
            reservation.Id,
            StoreId,
            OfferId,
            reservation.Quantity,
            reservation.ReferenceType.ToString(),
            reservation.ReferenceId));
    }

    public bool CanReserve(int quantity) => CanReserveAt(quantity, DateTime.UtcNow);

    public bool CanReserveAt(int quantity, DateTime utcNow) =>
        !TrackInventory || quantity <= GetAvailableAt(utcNow) || AllowBackorder;

    public InventoryAvailabilityStatus GetAvailabilityStatus() =>
        GetAvailabilityStatusAt(DateTime.UtcNow);

    public InventoryAvailabilityStatus GetAvailabilityStatusAt(DateTime utcNow) =>
        !TrackInventory
            ? InventoryAvailabilityStatus.NotTracked
            : GetAvailableAt(utcNow) >= 1
                ? InventoryAvailabilityStatus.InStock
                : AllowBackorder
                    ? InventoryAvailabilityStatus.Backorder
                    : InventoryAvailabilityStatus.OutOfStock;

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }
    }

    private static void ValidateOffer(int offerId)
    {
        if (offerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offerId));
        }
    }

    private static void ValidateProduct(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
    }
}
