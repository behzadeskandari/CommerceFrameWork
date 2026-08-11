namespace Commerce.Inventory.Domain.Enums;

public enum InventoryMovementType
{
    InitialStock = 0,
    PurchaseReceipt = 1,
    ManualAdjustment = 2,
    Return = 3,
    Correction = 4,
    Damage = 5,
    Loss = 6
}

public enum InventoryReferenceType
{
    None = 0,
    Order = 1,
    Manual = 2
}

public enum InventoryReservationStatus
{
    Active = 0,
    Released = 1,
    Converted = 2,
    Expired = 3,
    Cancelled = 4
}

public enum InventoryAvailabilityStatus
{
    NotTracked = 0,
    InStock = 1,
    OutOfStock = 2,
    Backorder = 3
}
