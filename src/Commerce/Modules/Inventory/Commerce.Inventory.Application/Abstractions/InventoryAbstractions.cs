using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;

namespace Commerce.Inventory.Application.Abstractions;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetByStoreAndOfferAsync(int storeId, int offerId, CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetByStoreOfferAndWarehouseAsync(
        int storeId,
        int offerId,
        int? warehouseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> ListByStoreAndOfferAsync(
        int storeId,
        int offerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> ListByStoreAndOfferForUpdateAsync(
        int storeId,
        int offerId,
        CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetByStoreAndOfferForUpdateAsync(int storeId, int offerId, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task SaveAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryItem> Items, int TotalCount)> ListAsync(
        InventoryListCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryReservation>> GetActiveReservationsForReferenceAsync(
        InventoryReferenceType referenceType,
        int referenceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryReservation>> GetExpiredActiveReservationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<InventoryReservation?> GetReservationByIdAsync(int reservationId, CancellationToken cancellationToken = default);

    Task<Warehouse?> GetWarehouseByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Warehouse?> GetDefaultWarehouseAsync(int storeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warehouse>> ListWarehousesAsync(int storeId, CancellationToken cancellationToken = default);

    Task AddWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default);

    Task SaveWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default);

    Task<StockLocation?> GetStockLocationByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockLocation>> ListStockLocationsAsync(int warehouseId, CancellationToken cancellationToken = default);

    Task AddStockLocationAsync(StockLocation location, CancellationToken cancellationToken = default);

    Task SaveStockLocationAsync(StockLocation location, CancellationToken cancellationToken = default);

    Task ClearDefaultWarehouseAsync(int storeId, int exceptWarehouseId, CancellationToken cancellationToken = default);
}

public sealed record InventoryListCriteria(
    int Page,
    int PageSize,
    int? StoreId,
    int? OfferId,
    int? ProductId,
    int? WarehouseId,
    InventoryAvailabilityStatus? AvailabilityStatus);

public sealed record AggregatedOfferAvailability(
    bool TrackInventory,
    bool AllowBackorder,
    int OnHand,
    int Reserved,
    int Incoming,
    int Available,
    bool IsLowStock,
    InventoryAvailabilityStatus Status,
    int PrimaryInventoryItemId);

public static class InventoryAvailabilityAggregator
{
    public static AggregatedOfferAvailability Aggregate(IReadOnlyList<InventoryItem> items, DateTime utcNow)
    {
        if (items.Count == 0)
        {
            return new AggregatedOfferAvailability(
                false,
                false,
                0,
                0,
                0,
                int.MaxValue,
                false,
                InventoryAvailabilityStatus.NotTracked,
                0);
        }

        var tracked = items.Where(x => x.TrackInventory).ToList();
        if (tracked.Count == 0)
        {
            var first = items[0];
            return new AggregatedOfferAvailability(
                false,
                first.AllowBackorder,
                0,
                0,
                0,
                int.MaxValue,
                false,
                InventoryAvailabilityStatus.NotTracked,
                first.Id);
        }

        var onHand = tracked.Sum(x => x.OnHand);
        var reserved = tracked.Sum(x => x.GetActiveReservedQuantity(utcNow));
        var incoming = tracked.Sum(x => x.Incoming);
        var available = Math.Max(0, onHand - reserved);
        var allowBackorder = tracked.Any(x => x.AllowBackorder);
        var isLowStock = tracked.Any(x => x.IsLowStock(utcNow));
        var primary = tracked.OrderByDescending(x => x.GetAvailableAt(utcNow)).First();

        var status = !tracked.Any(x => x.TrackInventory)
            ? InventoryAvailabilityStatus.NotTracked
            : available >= 1
                ? InventoryAvailabilityStatus.InStock
                : allowBackorder
                    ? InventoryAvailabilityStatus.Backorder
                    : InventoryAvailabilityStatus.OutOfStock;

        return new AggregatedOfferAvailability(
            true,
            allowBackorder,
            onHand,
            reserved,
            incoming,
            available,
            isLowStock,
            status,
            primary.Id);
    }
}
