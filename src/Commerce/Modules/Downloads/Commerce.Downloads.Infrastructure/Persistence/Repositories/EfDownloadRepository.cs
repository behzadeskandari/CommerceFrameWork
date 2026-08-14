using Commerce.Downloads.Application.Abstractions;
using Commerce.Downloads.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Downloads.Infrastructure.Persistence.Repositories;

public sealed class EfDownloadRepository(CommerceDbContext dbContext) : IDownloadRepository
{
    public Task<ProductDownloadSettings?> GetSettingsAsync(int productId, int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductDownloadSettings>()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.StoreId == storeId, cancellationToken);

    public Task AddSettingsAsync(ProductDownloadSettings settings, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductDownloadSettings>().Add(settings);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductDownloadFile>> ListFilesAsync(
        int productId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<ProductDownloadFile>()
            .Where(x => x.ProductId == productId && x.StoreId == storeId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<ProductDownloadFile?> GetFileAsync(int fileId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductDownloadFile>().FirstOrDefaultAsync(x => x.Id == fileId, cancellationToken);

    public Task AddFileAsync(ProductDownloadFile file, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductDownloadFile>().Add(file);
        return Task.CompletedTask;
    }

    public Task RemoveFileAsync(ProductDownloadFile file, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductDownloadFile>().Remove(file);
        return Task.CompletedTask;
    }

    public Task<bool> EntitlementExistsForOrderItemAsync(int orderItemId, CancellationToken cancellationToken = default) =>
        dbContext.Set<DownloadEntitlement>().AnyAsync(x => x.OrderItemId == orderItemId, cancellationToken);

    public Task AddEntitlementAsync(DownloadEntitlement entitlement, CancellationToken cancellationToken = default)
    {
        dbContext.Set<DownloadEntitlement>().Add(entitlement);
        return Task.CompletedTask;
    }

    public Task<DownloadEntitlement?> GetEntitlementAsync(int entitlementId, CancellationToken cancellationToken = default) =>
        dbContext.Set<DownloadEntitlement>().FirstOrDefaultAsync(x => x.Id == entitlementId, cancellationToken);

    public async Task<IReadOnlyList<DownloadEntitlement>> ListEntitlementsForCustomerAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<DownloadEntitlement>()
            .Where(x => x.CustomerId == customerId && x.StoreId == storeId && !x.IsRevoked)
            .OrderByDescending(x => x.GrantedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task AddHistoryAsync(DownloadHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.Set<DownloadHistoryEntry>().Add(entry);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<DownloadHistoryEntry>> ListHistoryForProductAsync(
        int productId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<DownloadHistoryEntry>()
            .Join(
                dbContext.Set<DownloadEntitlement>(),
                history => history.EntitlementId,
                entitlement => entitlement.Id,
                (history, entitlement) => new { history, entitlement })
            .Where(x => x.entitlement.ProductId == productId && x.entitlement.StoreId == storeId)
            .Select(x => x.history)
            .OrderByDescending(x => x.DownloadedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
