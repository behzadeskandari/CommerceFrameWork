using Commerce.Store.Application.Abstractions;
using Commerce.Store.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Store.Infrastructure.Persistence.Repositories;

internal sealed class EfStoreRepository(CommerceDbContext dbContext) : IStoreRepository
{
    public Task<StoreEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<StoreEntity>()
            .Include(x => x.Domains)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<StoreEntity?> GetBySystemNameAsync(string systemName, CancellationToken cancellationToken = default) =>
        dbContext.Set<StoreEntity>()
            .Include(x => x.Domains)
            .FirstOrDefaultAsync(
                x => x.SystemName == systemName.Trim() && !x.IsDeleted,
                cancellationToken);

    public Task<StoreEntity?> GetDefaultActiveAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<StoreEntity>()
            .Include(x => x.Domains)
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<StoreEntity?> FindByHostAsync(
        string host,
        int? port,
        CancellationToken cancellationToken = default)
    {
        var normalizedHost = host.Trim().ToLowerInvariant();
        var query = dbContext.Set<StoreDomain>()
            .AsNoTracking()
            .Where(x => x.Host == normalizedHost);

        if (port.HasValue)
        {
            query = query.Where(x => x.Port == port || x.Port == null);
        }

        var domain = await query
            .OrderByDescending(x => x.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (domain is null)
        {
            return null;
        }

        return await dbContext.Set<StoreEntity>()
            .Include(x => x.Domains)
            .FirstOrDefaultAsync(x => x.Id == domain.StoreId && x.IsActive && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoreEntity>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<StoreEntity>()
            .Include(x => x.Domains)
            .Where(x => !x.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(StoreEntity store, CancellationToken cancellationToken = default)
    {
        dbContext.Set<StoreEntity>().Add(store);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(StoreEntity store, CancellationToken cancellationToken = default)
    {
        dbContext.Set<StoreEntity>().Update(store);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfLanguageRepository(CommerceDbContext dbContext) : ILanguageRepository
{
    public Task<Language?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Language>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Language?> GetByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken = default) =>
        dbContext.Set<Language>()
            .FirstOrDefaultAsync(x => x.LanguageCode == languageCode.Trim().ToLowerInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Language>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Language>().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Language language, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Language>().Add(language);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Language language, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Language>().Update(language);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfStoreCurrencyRepository(CommerceDbContext dbContext) : IStoreCurrencyRepository
{
    public Task<StoreCurrency?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<StoreCurrency>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<StoreCurrency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.Set<StoreCurrency>()
            .FirstOrDefaultAsync(x => x.Code == code.Trim().ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<StoreCurrency>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<StoreCurrency>().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(StoreCurrency currency, CancellationToken cancellationToken = default)
    {
        dbContext.Set<StoreCurrency>().Add(currency);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(StoreCurrency currency, CancellationToken cancellationToken = default)
    {
        dbContext.Set<StoreCurrency>().Update(currency);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
