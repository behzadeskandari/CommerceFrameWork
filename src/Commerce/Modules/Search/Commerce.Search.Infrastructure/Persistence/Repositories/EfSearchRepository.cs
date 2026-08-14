using Commerce.Search.Application.Abstractions;
using Commerce.Search.Domain.Entities;
using Commerce.Search.Domain.Enums;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Search.Infrastructure.Persistence.Repositories;

public sealed class EfSearchRepository(CommerceDbContext dbContext) : ISearchRepository
{
    public Task<SearchIndexEntry?> GetEntryAsync(int productId, int storeId, int languageId, CancellationToken cancellationToken = default) =>
        dbContext.Set<SearchIndexEntry>().FirstOrDefaultAsync(
            x => x.ProductId == productId && x.StoreId == storeId && x.LanguageId == languageId,
            cancellationToken);

    public async Task UpsertEntryAsync(SearchIndexEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await GetEntryAsync(entry.ProductId, entry.StoreId, entry.LanguageId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            dbContext.Set<SearchIndexEntry>().Add(entry);
        }
        else
        {
            existing.UpdateFrom(entry);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteEntryAsync(int productId, int storeId, int languageId, CancellationToken cancellationToken = default)
    {
        var existing = await GetEntryAsync(productId, storeId, languageId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        dbContext.Set<SearchIndexEntry>().Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteEntriesForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.Set<SearchIndexEntry>().Where(x => x.ProductId == productId).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entries.Count == 0)
        {
            return;
        }

        dbContext.Set<SearchIndexEntry>().RemoveRange(entries);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAllEntriesAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<SearchIndexEntry>().ExecuteDeleteAsync(cancellationToken);

    public Task<int> CountEntriesAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<SearchIndexEntry>().CountAsync(cancellationToken);

    public Task<DateTime?> GetLastIndexedAtUtcAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<SearchIndexEntry>().MaxAsync(x => (DateTime?)x.IndexedAtUtc, cancellationToken);

    public async Task AddJobAsync(SearchIndexJob job, CancellationToken cancellationToken = default)
    {
        dbContext.Set<SearchIndexJob>().Add(job);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<SearchIndexJob>> ListPendingJobsAsync(int batchSize, CancellationToken cancellationToken = default) =>
        dbContext.Set<SearchIndexJob>()
            .Where(x => x.Status == SearchIndexJobStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<SearchIndexJob>)t.Result, cancellationToken);

    public async Task SaveJobAsync(SearchIndexJob job, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountJobsByStatusAsync(SearchIndexJobStatus status, CancellationToken cancellationToken = default) =>
        dbContext.Set<SearchIndexJob>().CountAsync(x => x.Status == status, cancellationToken);
}
