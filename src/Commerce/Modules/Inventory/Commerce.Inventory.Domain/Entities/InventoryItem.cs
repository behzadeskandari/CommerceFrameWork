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

    public int? StockLocationId { get; private set; }

    public bool TrackInventory { get; private set; }

    public bool AllowBackorder { get; private set; }

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Incoming { get; private set; }

    public int? LowStockThreshold { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<InventoryMovement> Movements => _movements;

    public IReadOnlyCollection<InventoryReservation> Reservations => _reservations;

    public int Available => Math.Max(0, OnHand - Reserved);

    public int TotalSupply => OnHand + Incoming;

    public bool IsLowStock(DateTime utcNow) =>
        TrackInventory &&
        LowStockThreshold.HasValue &&
        GetAvailableAt(utcNow) <= LowStockThreshold.Value;

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
        int? warehouseId = null,
        int? stockLocationId = null,
        int? lowStockThreshold = null)
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
            StockLocationId = stockLocationId,
            TrackInventory = trackInventory,
            AllowBackorder = allowBackorder,
            OnHand = 0,
            Reserved = 0,
            Incoming = 0,
            LowStockThreshold = lowStockThreshold,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void SetLowStockThreshold(int? threshold)
    {
        if (threshold is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }

        LowStockThreshold = threshold;
        Touch();
    }

    public void SetStockLocation(int? stockLocationId)
    {
        StockLocationId = stockLocationId;
        Touch();
    }

    public InventoryMovement ReceiveIncoming(
        int quantity,
        string reason,
        InventoryReferenceType referenceType,
        int? referenceId,
        string? createdBy)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (Incoming < quantity)
        {
            throw new InvalidOperationException("Cannot receive more than incoming quantity.");
        }

        Incoming -= quantity;
        return AdjustOnHand(quantity, InventoryMovementType.PurchaseReceipt, reason, referenceType, referenceId, createdBy);
    }

    public void AddIncoming(int quantity, string reason, string? createdBy)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        Incoming += quantity;
        Touch();
        RaiseDomainEvent(new InventoryAdjustedEvent(Id, StoreId, OfferId, quantity, "Incoming"));
    }

    public InventoryMovement TransferOut(
        int quantity,
        int transferReferenceId,
        string reason,
        string? createdBy)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (GetAvailableAt(DateTime.UtcNow) < quantity)
        {
            throw new InvalidOperationException("Insufficient available stock for transfer.");
        }

        return AdjustOnHand(
            -quantity,
            InventoryMovementType.TransferOut,
            reason,
            InventoryReferenceType.Transfer,
            transferReferenceId,
            createdBy);
    }

    public InventoryMovement TransferIn(
        int quantity,
        int transferReferenceId,
        string reason,
        string? createdBy)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return AdjustOnHand(
            quantity,
            InventoryMovementType.TransferIn,
            reason,
            InventoryReferenceType.Transfer,
            transferReferenceId,
            createdBy);
    }

    public InventoryMovement ConvertReservationToSale(
        InventoryReservation reservation,
        DateTime utcNow,
        string? createdBy)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        if (reservation.InventoryItemId != Id)
        {
            throw new InvalidOperationException("Reservation does not belong to this inventory item.");
        }

        if (!reservation.IsActive(utcNow))
        {
            throw new InvalidOperationException("Reservation is not active.");
        }

        reservation.Convert(utcNow);
        Reserved = Math.Max(0, Reserved - reservation.Quantity);

        var deduct = Math.Min(reservation.Quantity, OnHand);
        InventoryMovement? movement = null;
        if (deduct > 0)
        {
            movement = AdjustOnHand(
                -deduct,
                InventoryMovementType.Sale,
                "Reservation converted to sale.",
                reservation.ReferenceType,
                reservation.ReferenceId,
                createdBy);
        }

        Touch();
        RaiseDomainEvent(new InventoryReservationConvertedEvent(
            Id,
            reservation.Id,
            StoreId,
            OfferId,
            reservation.Quantity));

        return movement ?? InventoryMovement.Create(
            Id,
            0,
            InventoryMovementType.Sale,
            "Backorder conversion without on-hand deduction.",
            reservation.ReferenceType,
            reservation.ReferenceId,
            createdBy);
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

    public void ReleaseReservedQuantity(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        Reserved = Math.Max(0, Reserved - quantity);
        Touch();
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
