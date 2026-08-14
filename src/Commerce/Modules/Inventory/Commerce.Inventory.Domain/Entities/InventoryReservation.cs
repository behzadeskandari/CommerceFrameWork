using Commerce.Framework.Core.Entities;
using Commerce.Inventory.Domain.Enums;

namespace Commerce.Inventory.Domain.Entities;

public sealed class InventoryReservation : Entity
{
    public const int ReasonMaxLength = 500;

    private InventoryReservation()
    {
    }

    public int InventoryItemId { get; private set; }

    public int Quantity { get; private set; }

    public InventoryReferenceType ReferenceType { get; private set; }

    public int ReferenceId { get; private set; }

    public InventoryReservationStatus Status { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public string? ReleaseReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static InventoryReservation Create(
        int inventoryItemId,
        int quantity,
        InventoryReferenceType referenceType,
        int referenceId,
        DateTime expiresAtUtc,
        DateTime utcNow)
    {
        if (inventoryItemId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inventoryItemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (referenceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceId));
        }

        return new InventoryReservation
        {
            InventoryItemId = inventoryItemId,
            Quantity = quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Status = InventoryReservationStatus.Active,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public bool IsActive(DateTime utcNow) =>
        Status == InventoryReservationStatus.Active && utcNow < ExpiresAtUtc;

    public void Release(string reason, DateTime utcNow)
    {
        if (Status is InventoryReservationStatus.Released or InventoryReservationStatus.Cancelled)
        {
            return;
        }

        if (Status is InventoryReservationStatus.Converted)
        {
            throw new InvalidOperationException("Converted reservations cannot be released.");
        }

        Status = InventoryReservationStatus.Released;
        ReleaseReason = string.IsNullOrWhiteSpace(reason) ? "Released." : reason.Trim();
        UpdatedAtUtc = utcNow;
    }

    public void Convert(DateTime utcNow)
    {
        if (Status != InventoryReservationStatus.Active)
        {
            throw new InvalidOperationException($"Reservation cannot be converted from status {Status}.");
        }

        Status = InventoryReservationStatus.Converted;
        UpdatedAtUtc = utcNow;
    }

    public void MarkExpired(DateTime utcNow)
    {
        if (Status != InventoryReservationStatus.Active)
        {
            return;
        }

        Status = InventoryReservationStatus.Expired;
        ReleaseReason = "Expired.";
        UpdatedAtUtc = utcNow;
    }

    public void ReduceQuantity(int reduceBy, DateTime utcNow)
    {
        if (reduceBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reduceBy));
        }

        if (reduceBy >= Quantity)
        {
            throw new InvalidOperationException("Use Release for full reservation release.");
        }

        if (Status != InventoryReservationStatus.Active)
        {
            throw new InvalidOperationException($"Reservation cannot be reduced from status {Status}.");
        }

        Quantity -= reduceBy;
        UpdatedAtUtc = utcNow;
    }
}
