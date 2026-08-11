namespace Commerce.Inventory.Domain.Events;

public sealed record InventoryAdjustedEvent(
    int InventoryItemId,
    int StoreId,
    int OfferId,
    int QuantityDelta,
    string MovementType) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record InventoryReservedEvent(
    int InventoryItemId,
    int ReservationId,
    int StoreId,
    int OfferId,
    int Quantity,
    string ReferenceType,
    int ReferenceId) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record InventoryReservationReleasedEvent(
    int InventoryItemId,
    int ReservationId,
    int StoreId,
    int OfferId,
    int Quantity,
    string ReferenceType,
    int ReferenceId) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record InventoryReservationConvertedEvent(
    int InventoryItemId,
    int ReservationId,
    int StoreId,
    int OfferId,
    int Quantity) : Commerce.Framework.Core.Events.DomainEvent;

public sealed record InventoryUnavailableEvent(
    int StoreId,
    int OfferId,
    int RequestedQuantity,
    int AvailableQuantity) : Commerce.Framework.Core.Events.DomainEvent;
