using Commerce.Framework.Data.Db;

using Commerce.Tax.Application.Abstractions;

using Commerce.Tax.Domain.Entities;

using Microsoft.EntityFrameworkCore;



namespace Commerce.Tax.Infrastructure.Persistence.Repositories;



public sealed class EfTaxRepository(CommerceDbContext dbContext) : ITaxRepository

{

    public Task<IReadOnlyList<TaxCategory>> GetActiveCategoriesAsync(int storeId, CancellationToken cancellationToken = default) =>

        dbContext.Set<TaxCategory>()

            .AsNoTracking()

            .Where(x => x.StoreId == storeId && x.IsActive && !x.IsDeleted)

            .OrderBy(x => x.DisplayOrder)

            .ToListAsync(cancellationToken)

            .ContinueWith(t => (IReadOnlyList<TaxCategory>)t.Result, cancellationToken);



    public Task<TaxCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default) =>

        dbContext.Set<TaxCategory>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);



    public Task<IReadOnlyList<TaxCategory>> ListCategoriesAsync(int? storeId, CancellationToken cancellationToken = default)

    {

        var query = dbContext.Set<TaxCategory>().AsNoTracking().AsQueryable();

        if (storeId.HasValue)

        {

            query = query.Where(x => x.StoreId == storeId.Value);

        }



        return query.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken)

            .ContinueWith(t => (IReadOnlyList<TaxCategory>)t.Result, cancellationToken);

    }



    public async Task AddCategoryAsync(TaxCategory category, CancellationToken cancellationToken = default)

    {

        dbContext.Set<TaxCategory>().Add(category);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public async Task SaveCategoryAsync(TaxCategory category, CancellationToken cancellationToken = default)

    {

        dbContext.Set<TaxCategory>().Update(category);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public async Task<IReadOnlyList<TaxZone>> GetActiveZonesAsync(int storeId, CancellationToken cancellationToken = default)

    {

        var zones = await dbContext.Set<TaxZone>()

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



    public async Task<TaxZone?> GetZoneByIdAsync(int id, CancellationToken cancellationToken = default)

    {

        var zone = await dbContext.Set<TaxZone>()

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



    public async Task<IReadOnlyList<TaxZone>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default)

    {

        var query = dbContext.Set<TaxZone>()

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



    public async Task AddZoneAsync(TaxZone zone, CancellationToken cancellationToken = default)

    {

        dbContext.Set<TaxZone>().Add(zone);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public async Task SaveZoneAsync(TaxZone zone, CancellationToken cancellationToken = default)

    {

        dbContext.Set<TaxZone>().Update(zone);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public Task<IReadOnlyList<TaxRate>> GetActiveRatesAsync(int storeId, CancellationToken cancellationToken = default) =>

        dbContext.Set<TaxRate>()

            .AsNoTracking()

            .Where(x => x.StoreId == storeId && x.IsActive && !x.IsDeleted)

            .ToListAsync(cancellationToken)

            .ContinueWith(t => (IReadOnlyList<TaxRate>)t.Result, cancellationToken);



    public Task<TaxRate?> GetRateByIdAsync(int id, CancellationToken cancellationToken = default) =>

        dbContext.Set<TaxRate>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);



    public Task<IReadOnlyList<TaxRate>> ListRatesAsync(int? storeId, int? categoryId, CancellationToken cancellationToken = default)

    {

        var query = dbContext.Set<TaxRate>().AsNoTracking().AsQueryable();

        if (storeId.HasValue)

        {

            query = query.Where(x => x.StoreId == storeId.Value);

        }



        if (categoryId.HasValue)

        {

            query = query.Where(x => x.TaxCategoryId == categoryId.Value);

        }



        return query.OrderByDescending(x => x.Priority).ToListAsync(cancellationToken)

            .ContinueWith(t => (IReadOnlyList<TaxRate>)t.Result, cancellationToken);

    }



    public async Task AddRateAsync(TaxRate rate, CancellationToken cancellationToken = default)

    {

        dbContext.Set<TaxRate>().Add(rate);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }



    public async Task SaveRateAsync(TaxRate rate, CancellationToken cancellationToken = default)

    {

        dbContext.Set<TaxRate>().Update(rate);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }

}


