using Commerce.Framework.Core.Entities;

namespace Commerce.Seo.Domain.Entities;

public sealed class SeoMetadata : AggregateRoot
{
    public const int EntityNameMaxLength = 128;
    public const int MetaTitleMaxLength = 200;
    public const int MetaDescriptionMaxLength = 1000;
    public const int MetaKeywordsMaxLength = 500;
    public const int CanonicalUrlMaxLength = 500;
    public const int StructuredDataMaxLength = 8000;

    public string EntityName { get; private set; } = string.Empty;

    public int EntityId { get; private set; }

    public int? LanguageId { get; private set; }

    public int? StoreId { get; private set; }

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public string? MetaKeywords { get; private set; }

    public string? CanonicalUrl { get; private set; }

    public string? StructuredDataJson { get; private set; }

    public static SeoMetadata Create(
        string entityName,
        int entityId,
        int? languageId,
        int? storeId,
        string? metaTitle,
        string? metaDescription,
        string? metaKeywords,
        string? canonicalUrl,
        string? structuredDataJson)
    {
        if (string.IsNullOrWhiteSpace(entityName) || entityId <= 0)
        {
            throw new ArgumentException("Entity name and id are required.");
        }

        return new SeoMetadata
        {
            EntityName = entityName.Trim(),
            EntityId = entityId,
            LanguageId = languageId,
            StoreId = storeId,
            MetaTitle = Trim(metaTitle, MetaTitleMaxLength),
            MetaDescription = Trim(metaDescription, MetaDescriptionMaxLength),
            MetaKeywords = Trim(metaKeywords, MetaKeywordsMaxLength),
            CanonicalUrl = Trim(canonicalUrl, CanonicalUrlMaxLength),
            StructuredDataJson = Trim(structuredDataJson, StructuredDataMaxLength)
        };
    }

    public void Update(
        string? metaTitle,
        string? metaDescription,
        string? metaKeywords,
        string? canonicalUrl,
        string? structuredDataJson)
    {
        MetaTitle = Trim(metaTitle, MetaTitleMaxLength);
        MetaDescription = Trim(metaDescription, MetaDescriptionMaxLength);
        MetaKeywords = Trim(metaKeywords, MetaKeywordsMaxLength);
        CanonicalUrl = Trim(canonicalUrl, CanonicalUrlMaxLength);
        StructuredDataJson = Trim(structuredDataJson, StructuredDataMaxLength);
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
