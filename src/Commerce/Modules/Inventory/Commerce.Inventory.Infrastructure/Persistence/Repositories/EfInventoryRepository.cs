using Commerce.Framework.Data.Db;
using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Inventory.Infrastructure.Persistence.Repositories;

public sealed class EfInventoryRepository(CommerceDbContext dbContext) : IInventoryRepository
{
    public Task<InventoryItem?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<InventoryItem>()
            .Include(x => x.Movements)
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InventoryItem?> GetByStoreAndOfferAsync(int storeId, int offerId, CancellationToken cancellationToken = default) =>
        dbContext.Set<InventoryItem>()
            .AsNoTracking()
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.OfferId == offerId, cancellationToken);

    public Task<InventoryItem?> GetByStoreAndOfferForUpdateAsync(int storeId, int offerId, CancellationToken cancellationToken = default) =>
        dbContext.Set<InventoryItem>()
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.OfferId == offerId, cancellationToken);

    public async Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        dbContext.Set<InventoryItem>().Add(item);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        dbContext.Set<InventoryItem>().Update(item);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<InventoryItem> Items, int TotalCount)> ListAsync(
        InventoryListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<InventoryItem>().AsNoTracking().AsQueryable();

        if (criteria.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.OfferId.HasValue)
        {
            query = query.Where(x => x.OfferId == criteria.OfferId.Value);
        }

        if (criteria.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == criteria.ProductId.Value);
        }

        var items = await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (criteria.AvailabilityStatus.HasValue)
        {
            items = items
                .Where(x => x.GetAvailabilityStatus() == criteria.AvailabilityStatus.Value)
                .ToList();
        }

        var total = items.Count;
        var pageItems = items
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return (pageItems, total);
    }

    public async Task<IReadOnlyList<InventoryReservation>> GetActiveReservationsForReferenceAsync(
        InventoryReferenceType referenceType,
        int referenceId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        return await dbContext.Set<InventoryReservation>()
            .Where(x =>
                x.ReferenceType == referenceType &&
                x.ReferenceId == referenceId &&
                x.Status == InventoryReservationStatus.Active &&
                x.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<IReadOnlyList<InventoryReservation>> GetExpiredActiveReservationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<InventoryReservation>()
            .Where(x =>
                x.Status == InventoryReservationStatus.Active &&
                x.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<InventoryReservation>)t.Result, cancellationToken);

    public Task<InventoryReservation?> GetReservationByIdAsync(int reservationId, CancellationToken cancellationToken = default) =>
        dbContext.Set<InventoryReservation>()
            .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);
}
