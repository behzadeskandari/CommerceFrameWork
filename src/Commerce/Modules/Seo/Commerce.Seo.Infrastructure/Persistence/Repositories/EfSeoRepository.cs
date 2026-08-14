using Commerce.Seo.Application.Abstractions;
using Commerce.Seo.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Seo.Infrastructure.Persistence.Repositories;

public sealed class EfSeoRepository(CommerceDbContext dbContext) : ISeoRepository
{
    public Task<UrlRecord?> GetUrlRecordBySlugAsync(string slug, int? languageId, int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<UrlRecord>()
            .FirstOrDefaultAsync(x =>
                x.Slug == slug &&
                x.IsActive &&
                (!x.StoreId.HasValue || x.StoreId.Value == storeId) &&
                (!languageId.HasValue || !x.LanguageId.HasValue || x.LanguageId.Value == languageId.Value),
                cancellationToken);

    public Task<UrlRecord?> GetUrlRecordAsync(string entityName, int entityId, int? languageId, int? storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<UrlRecord>()
            .FirstOrDefaultAsync(x =>
                x.EntityName == entityName &&
                x.EntityId == entityId &&
                x.LanguageId == languageId &&
                x.StoreId == storeId,
                cancellationToken);

    public Task<IReadOnlyList<UrlRecord>> ListUrlRecordsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<UrlRecord>().AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => !x.StoreId.HasValue || x.StoreId.Value == storeId.Value);
        }

        return query.OrderBy(x => x.Slug).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<UrlRecord>)t.Result, cancellationToken);
    }

    public async Task AddUrlRecordAsync(UrlRecord record, CancellationToken cancellationToken = default) =>
        dbContext.Set<UrlRecord>().Add(record);

    public async Task SaveUrlRecordAsync(UrlRecord record, CancellationToken cancellationToken = default) =>
        dbContext.Set<UrlRecord>().Update(record);

    public Task<SeoMetadata?> GetMetadataAsync(string entityName, int entityId, int? languageId, int? storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<SeoMetadata>()
            .FirstOrDefaultAsync(x =>
                x.EntityName == entityName &&
                x.EntityId == entityId &&
                x.LanguageId == languageId &&
                x.StoreId == storeId,
                cancellationToken);

    public async Task AddMetadataAsync(SeoMetadata metadata, CancellationToken cancellationToken = default) =>
        dbContext.Set<SeoMetadata>().Add(metadata);

    public async Task SaveMetadataAsync(SeoMetadata metadata, CancellationToken cancellationToken = default) =>
        dbContext.Set<SeoMetadata>().Update(metadata);

    public Task<SeoSettings?> GetSettingsAsync(int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<SeoSettings>().FirstOrDefaultAsync(x => x.StoreId == storeId, cancellationToken);

    public async Task AddSettingsAsync(SeoSettings settings, CancellationToken cancellationToken = default) =>
        dbContext.Set<SeoSettings>().Add(settings);

    public async Task SaveSettingsAsync(SeoSettings settings, CancellationToken cancellationToken = default) =>
        dbContext.Set<SeoSettings>().Update(settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
