using Commerce.Framework.Data.Db;
using Commerce.Media.Application.Abstractions;
using Commerce.Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Media.Infrastructure.Persistence.Repositories;

public sealed class EfMediaAssetRepository(CommerceDbContext dbContext) : IMediaAssetRepository
{
    public Task<MediaAsset?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<MediaAsset>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MediaAsset>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<MediaAsset>()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MediaAsset>> ListAsync(
        int storeId,
        string? term = null,
        Domain.Enums.MediaType? mediaType = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<MediaAsset>().AsQueryable().Where(x => x.StoreId == storeId && !x.IsDeleted);

        if (mediaType.HasValue)
        {
            query = query.Where(x => x.MediaType == mediaType.Value);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var normalized = term.Trim();
            query = query.Where(x =>
                x.FileName.Contains(normalized) ||
                x.OriginalFileName.Contains(normalized) ||
                (x.Title != null && x.Title.Contains(normalized)));
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        dbContext.Set<MediaAsset>().Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        dbContext.Set<MediaAsset>().Update(asset);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
