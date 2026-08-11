using Commerce.Framework.Core.Errors;

namespace Commerce.Inventory.Application.Inventory;

public static class InventoryErrors
{
    public static Error NotFound(int inventoryItemId) =>
        Error.NotFound($"Inventory item '{inventoryItemId}' was not found.");

    public static Error ItemNotFound(int inventoryItemId) => NotFound(inventoryItemId);

    public static Error OfferNotFound(int offerId) =>
        Error.NotFound($"Offer '{offerId}' was not found.");

    public static Error InsufficientInventory(int offerId) =>
        Error.Validation($"Insufficient inventory for offer '{offerId}'.");

    public static Error InsufficientInventory(int offerId, string message) =>
        Error.Validation(string.IsNullOrWhiteSpace(message)
            ? $"Insufficient inventory for offer '{offerId}'."
            : message);

    public static Error InventoryUnavailable(string message) =>
        Error.Validation(message);

    public static Error AlreadyExists(int storeId, int offerId) =>
        Error.Conflict($"Inventory already exists for store '{storeId}' and offer '{offerId}'.");

    public static Error ReservationNotFound(int reservationId) =>
        Error.NotFound($"Reservation '{reservationId}' was not found.");

    public static Error InvalidReservationState(int reservationId) =>
        Error.Validation($"Reservation '{reservationId}' is not in a valid state for this operation.");

    public static Error InvalidAdjustment(string message) =>
        Error.Validation(message);

    public static Error StoreMismatch() =>
        Error.Validation("Inventory does not belong to the current store.");
}
