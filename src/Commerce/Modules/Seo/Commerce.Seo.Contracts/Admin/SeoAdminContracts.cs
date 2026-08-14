using Commerce.Framework.Core.Results;

namespace Commerce.Seo.Contracts.Admin;

public sealed record UrlRecordDto(
    int Id,
    string EntityName,
    int EntityId,
    string Slug,
    int? LanguageId,
    int? StoreId,
    bool IsActive);

public sealed record SeoMetadataDto(
    int Id,
    string EntityName,
    int EntityId,
    int? LanguageId,
    int? StoreId,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    string? StructuredDataJson);

public sealed record SeoSettingsDto(
    int StoreId,
    string? DefaultMetaTitle,
    string? DefaultMetaDescription,
    string? RobotsTxt,
    bool SitemapEnabled);

public sealed record UpsertUrlRecordRequest(
    string EntityName,
    int EntityId,
    string Slug,
    int? LanguageId,
    int? StoreId,
    bool IsActive);

public sealed record UpsertSeoMetadataRequest(
    string EntityName,
    int EntityId,
    int? LanguageId,
    int? StoreId,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    string? StructuredDataJson);

public sealed record UpdateSeoSettingsRequest(
    string? DefaultMetaTitle,
    string? DefaultMetaDescription,
    string? RobotsTxt,
    bool SitemapEnabled);

public interface ISeoAdminService
{
    Task<Result<IReadOnlyList<UrlRecordDto>>> ListUrlRecordsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task<Result<UrlRecordDto>> UpsertUrlRecordAsync(UpsertUrlRecordRequest request, CancellationToken cancellationToken = default);

    Task<Result<SeoMetadataDto>> GetMetadataAsync(string entityName, int entityId, int? languageId, int? storeId, CancellationToken cancellationToken = default);

    Task<Result<SeoMetadataDto>> UpsertMetadataAsync(UpsertSeoMetadataRequest request, CancellationToken cancellationToken = default);

    Task<Result<SeoSettingsDto>> GetSettingsAsync(int storeId, CancellationToken cancellationToken = default);

    Task<Result<SeoSettingsDto>> UpdateSettingsAsync(int storeId, UpdateSeoSettingsRequest request, CancellationToken cancellationToken = default);
}
