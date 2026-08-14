using Commerce.Framework.Core.Results;

namespace Commerce.Seo.Contracts.Storefront;

public sealed record ResolvedUrlDto(
    string EntityName,
    int EntityId,
    string Slug);

public sealed record StorefrontSeoMetadataDto(
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    string? StructuredDataJson);

public interface ISeoStorefrontService
{
    Task<Result<ResolvedUrlDto>> ResolveSlugAsync(string slug, int? languageId, int storeId, CancellationToken cancellationToken = default);

    Task<Result<StorefrontSeoMetadataDto>> GetMetadataAsync(string entityName, int entityId, int? languageId, int storeId, CancellationToken cancellationToken = default);

    Task<Result<string>> GetRobotsTxtAsync(int storeId, CancellationToken cancellationToken = default);

    Task<Result<string>> GetSitemapXmlAsync(int storeId, string baseUrl, CancellationToken cancellationToken = default);
}
