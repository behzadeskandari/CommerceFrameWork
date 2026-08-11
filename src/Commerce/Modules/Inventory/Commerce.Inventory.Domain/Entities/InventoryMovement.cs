using Commerce.Framework.Core.Entities;
using Commerce.Inventory.Domain.Enums;

namespace Commerce.Inventory.Domain.Entities;

public sealed class InventoryMovement : Entity
{
    public const int ReasonMaxLength = 500;
    public const int CreatedByMaxLength = 200;

    private InventoryMovement()
    {
    }

    public int InventoryItemId { get; private set; }

    public int QuantityDelta { get; private set; }

    public InventoryMovementType MovementType { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public InventoryReferenceType ReferenceType { get; private set; }

    public int? ReferenceId { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static InventoryMovement Create(
        int inventoryItemId,
        int quantityDelta,
        InventoryMovementType movementType,
        string reason,
        InventoryReferenceType referenceType,
        int? referenceId,
        string? createdBy)
    {
        if (inventoryItemId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inventoryItemId));
        }

        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta));
        }

        return new InventoryMovement
        {
            InventoryItemId = inventoryItemId,
            QuantityDelta = quantityDelta,
            MovementType = movementType,
            Reason = string.IsNullOrWhiteSpace(reason) ? movementType.ToString() : Trim(reason, ReasonMaxLength),
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? null : Trim(createdBy, CreatedByMaxLength),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static string Trim(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
