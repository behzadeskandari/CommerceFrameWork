using Commerce.Seo.Domain.Entities;

namespace Commerce.Seo.Application.Abstractions;

public interface ISeoRepository
{
    Task<UrlRecord?> GetUrlRecordBySlugAsync(string slug, int? languageId, int storeId, CancellationToken cancellationToken = default);

    Task<UrlRecord?> GetUrlRecordAsync(string entityName, int entityId, int? languageId, int? storeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UrlRecord>> ListUrlRecordsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddUrlRecordAsync(UrlRecord record, CancellationToken cancellationToken = default);

    Task SaveUrlRecordAsync(UrlRecord record, CancellationToken cancellationToken = default);

    Task<SeoMetadata?> GetMetadataAsync(string entityName, int entityId, int? languageId, int? storeId, CancellationToken cancellationToken = default);

    Task AddMetadataAsync(SeoMetadata metadata, CancellationToken cancellationToken = default);

    Task SaveMetadataAsync(SeoMetadata metadata, CancellationToken cancellationToken = default);

    Task<SeoSettings?> GetSettingsAsync(int storeId, CancellationToken cancellationToken = default);

    Task AddSettingsAsync(SeoSettings settings, CancellationToken cancellationToken = default);

    Task SaveSettingsAsync(SeoSettings settings, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
