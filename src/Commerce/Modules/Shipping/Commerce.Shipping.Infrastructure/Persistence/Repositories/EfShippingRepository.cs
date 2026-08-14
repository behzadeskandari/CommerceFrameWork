using Commerce.Framework.Data.Db;
using Commerce.Shipping.Application.Abstractions;
using Commerce.Shipping.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Shipping.Infrastructure.Persistence.Repositories;

public sealed class EfShippingRepository(CommerceDbContext dbContext) : IShippingRepository
{
    public Task<IReadOnlyList<ShippingMethod>> GetActiveMethodsAsync(int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ShippingMethod>()
            .AsNoTracking()
            .Where(x => x.StoreId == storeId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ShippingMethod>)t.Result, cancellationToken);

    public Task<ShippingMethod?> GetMethodByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ShippingMethod>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<IReadOnlyList<ShippingMethod>> ListMethodsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ShippingMethod>().AsNoTracking().AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return query.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ShippingMethod>)t.Result, cancellationToken);
    }

    public async Task AddMethodAsync(ShippingMethod method, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShippingMethod>().Add(method);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveMethodAsync(ShippingMethod method, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShippingMethod>().Update(method);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ShippingZone>> GetActiveZonesAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var zones = await dbContext.Set<ShippingZone>()
            .AsNoTracking()
            .Include(x => x.Countries)
            .Include(x => x.States)
            .Include(x => x.PostalRules)
            .Where(x => x.StoreId == storeId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var zone in zones)
        {
            zone.LoadRules(zone.Countries, zone.States, zone.PostalRules);
        }

        return zones;
    }

    public async Task<ShippingZone?> GetZoneByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var zone = await dbContext.Set<ShippingZone>()
            .Include(x => x.Countries)
            .Include(x => x.States)
            .Include(x => x.PostalRules)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (zone is not null)
        {
            zone.LoadRules(zone.Countries, zone.States, zone.PostalRules);
        }

        return zone;
    }

    public async Task<IReadOnlyList<ShippingZone>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ShippingZone>()
            .AsNoTracking()
            .Include(x => x.Countries)
            .Include(x => x.States)
            .Include(x => x.PostalRules)
            .AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        var zones = await query.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var zone in zones)
        {
            zone.LoadRules(zone.Countries, zone.States, zone.PostalRules);
        }

        return zones;
    }

    public async Task AddZoneAsync(ShippingZone zone, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShippingZone>().Add(zone);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveZoneAsync(ShippingZone zone, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShippingZone>().Update(zone);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<ShippingRate>> GetActiveRatesAsync(int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ShippingRate>()
            .AsNoTracking()
            .Where(x => x.StoreId == storeId && x.IsActive && !x.IsDeleted)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ShippingRate>)t.Result, cancellationToken);

    public Task<ShippingRate?> GetRateByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ShippingRate>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<IReadOnlyList<ShippingRate>> ListRatesAsync(int? storeId, int? methodId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ShippingRate>().AsNoTracking().AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(x => x.ShippingMethodId == methodId.Value);
        }

        return query.ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ShippingRate>)t.Result, cancellationToken);
    }

    public async Task AddRateAsync(ShippingRate rate, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShippingRate>().Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveRateAsync(ShippingRate rate, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ShippingRate>().Update(rate);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<Shipment>> ListShipmentsByOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Shipment>()
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Shipment>)t.Result, cancellationToken);

    public async Task<Shipment?> GetShipmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var shipment = await dbContext.Set<Shipment>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return shipment;
    }

    public async Task AddShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Shipment>().Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Shipment>().Update(shipment);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<decimal> GetShippedQuantityForOrderItemAsync(int orderItemId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ShipmentItem>()
            .AsNoTracking()
            .Where(x => x.OrderItemId == orderItemId)
            .Join(
                dbContext.Set<Shipment>().Where(s =>
                    s.Status == Domain.Enums.ShipmentStatus.Shipped ||
                    s.Status == Domain.Enums.ShipmentStatus.Delivered),
                item => item.ShipmentId,
                shipment => shipment.Id,
                (item, _) => item.Quantity)
            .SumAsync(x => (decimal?)x, cancellationToken)
            .ContinueWith(t => t.Result ?? 0m, cancellationToken);
}
