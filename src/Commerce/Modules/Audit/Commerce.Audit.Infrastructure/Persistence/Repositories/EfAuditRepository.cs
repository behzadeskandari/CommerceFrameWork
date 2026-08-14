using Commerce.Audit.Application.Abstractions;
using Commerce.Audit.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Audit.Infrastructure.Persistence.Repositories;

public sealed class EfAuditRepository(CommerceDbContext dbContext) : IAuditRepository
{
    public async Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.Set<AuditEntry>().Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetLatestEntryHashAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<AuditEntry>().AsNoTracking();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        var latestHash = await query
            .OrderByDescending(x => x.Id)
            .Select(x => x.EntryHash)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(latestHash) ? AuditEntry.GenesisHash : latestHash;
    }

    public async Task<(IReadOnlyList<AuditEntry> Items, int TotalCount)> ListAsync(
        AuditListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<AuditEntry>().AsNoTracking();

        if (criteria.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.Category.HasValue)
        {
            query = query.Where(x => x.Category == criteria.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            query = query.Where(x => x.Action == criteria.Action);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ActorId))
        {
            query = query.Where(x => x.ActorId == criteria.ActorId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            query = query.Where(x => x.EntityType == criteria.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityId))
        {
            query = query.Where(x => x.EntityId == criteria.EntityId);
        }

        if (criteria.Success.HasValue)
        {
            query = query.Where(x => x.Success == criteria.Success.Value);
        }

        if (criteria.FromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= criteria.FromUtc.Value);
        }

        if (criteria.ToUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= criteria.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<AuditEntry>> ListForChainVerificationAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<AuditEntry>().AsNoTracking();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.Set<AuditEntry>()
            .Where(x => x.OccurredAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entries.Count == 0)
        {
            return 0;
        }

        dbContext.Set<AuditEntry>().RemoveRange(entries);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entries.Count;
    }
}
