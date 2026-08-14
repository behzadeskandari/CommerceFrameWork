using System.Text;
using System.Xml.Linq;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Seo.Application.Abstractions;
using Commerce.Seo.Contracts.Storefront;

namespace Commerce.Seo.Application.Storefront;

public sealed class SeoStorefrontService(ISeoRepository repository) : ISeoStorefrontService
{
    public async Task<Result<ResolvedUrlDto>> ResolveSlugAsync(string slug, int? languageId, int storeId, CancellationToken cancellationToken = default)
    {
        var record = await repository.GetUrlRecordBySlugAsync(slug, languageId, storeId, cancellationToken).ConfigureAwait(false);
        if (record is null || !record.IsActive)
        {
            return Result.Failure<ResolvedUrlDto>(Error.NotFound("URL not found."));
        }

        return Result.Success(new ResolvedUrlDto(record.EntityName, record.EntityId, record.Slug));
    }

    public async Task<Result<StorefrontSeoMetadataDto>> GetMetadataAsync(
        string entityName,
        int entityId,
        int? languageId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await repository.GetMetadataAsync(entityName, entityId, languageId, storeId, cancellationToken).ConfigureAwait(false);
        var settings = await repository.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);

        if (metadata is null && settings is null)
        {
            return Result.Success(new StorefrontSeoMetadataDto(null, null, null, null, null));
        }

        return Result.Success(new StorefrontSeoMetadataDto(
            metadata?.MetaTitle ?? settings?.DefaultMetaTitle,
            metadata?.MetaDescription ?? settings?.DefaultMetaDescription,
            metadata?.MetaKeywords,
            metadata?.CanonicalUrl,
            metadata?.StructuredDataJson));
    }

    public async Task<Result<string>> GetRobotsTxtAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        var content = settings?.RobotsTxt ?? "User-agent: *\nAllow: /";
        return Result.Success(content);
    }

    public async Task<Result<string>> GetSitemapXmlAsync(int storeId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (settings is not null && !settings.SitemapEnabled)
        {
            return Result.Failure<string>(Error.NotFound("Sitemap is disabled."));
        }

        var records = await repository.ListUrlRecordsAsync(storeId, cancellationToken).ConfigureAwait(false);
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var urlset = new XElement(ns + "urlset");

        foreach (var record in records.Where(x => x.IsActive))
        {
            var loc = BuildLocation(baseUrl, record.EntityName, record.Slug);
            urlset.Add(new XElement(ns + "url", new XElement(ns + "loc", loc)));
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            document.Save(writer);
        }

        return Result.Success(builder.ToString());
    }

    private static string BuildLocation(string baseUrl, string entityName, string slug)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        return entityName switch
        {
            "Product" => $"{trimmedBase}/product/{slug}",
            "Category" => $"{trimmedBase}/category/{slug}",
            "ContentPage" => $"{trimmedBase}/pages/{slug}",
            _ => $"{trimmedBase}/{slug}"
        };
    }
}
