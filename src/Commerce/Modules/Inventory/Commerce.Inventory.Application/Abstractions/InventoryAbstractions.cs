using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;

namespace Commerce.Inventory.Application.Abstractions;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetByStoreAndOfferAsync(int storeId, int offerId, CancellationToken cancellationToken = default);

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
}

public sealed record InventoryListCriteria(
    int Page,
    int PageSize,
    int? StoreId,
    int? OfferId,
    int? ProductId,
    InventoryAvailabilityStatus? AvailabilityStatus);
