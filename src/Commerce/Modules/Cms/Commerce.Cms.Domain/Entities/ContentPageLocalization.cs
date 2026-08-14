using Commerce.Framework.Core.Entities;

namespace Commerce.Cms.Domain.Entities;

public sealed class ContentPageLocalization : Entity
{
    public const int TitleMaxLength = 500;
    public const int SlugMaxLength = 200;
    public const int MetaTitleMaxLength = 500;
    public const int MetaDescriptionMaxLength = 1000;
    public const int MetaKeywordsMaxLength = 500;
    public const int CanonicalUrlMaxLength = 500;

    public int ContentPageId { get; private set; }

    public int LanguageId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public string? MetaKeywords { get; private set; }

    public string? CanonicalUrl { get; private set; }

    public static ContentPageLocalization Create(
        int contentPageId,
        int languageId,
        string title,
        string slug,
        string body,
        string? metaTitle,
        string? metaDescription,
        string? metaKeywords,
        string? canonicalUrl)
    {
        if (contentPageId <= 0 || languageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contentPageId));
        }

        return new ContentPageLocalization
        {
            ContentPageId = contentPageId,
            LanguageId = languageId,
            Title = NormalizeRequired(title, TitleMaxLength, nameof(title)),
            Slug = NormalizeSlug(slug),
            Body = body ?? string.Empty,
            MetaTitle = NormalizeOptional(metaTitle, MetaTitleMaxLength),
            MetaDescription = NormalizeOptional(metaDescription, MetaDescriptionMaxLength),
            MetaKeywords = NormalizeOptional(metaKeywords, MetaKeywordsMaxLength),
            CanonicalUrl = NormalizeOptional(canonicalUrl, CanonicalUrlMaxLength)
        };
    }

    public void Update(
        string title,
        string slug,
        string body,
        string? metaTitle,
        string? metaDescription,
        string? metaKeywords,
        string? canonicalUrl)
    {
        Title = NormalizeRequired(title, TitleMaxLength, nameof(title));
        Slug = NormalizeSlug(slug);
        Body = body ?? string.Empty;
        MetaTitle = NormalizeOptional(metaTitle, MetaTitleMaxLength);
        MetaDescription = NormalizeOptional(metaDescription, MetaDescriptionMaxLength);
        MetaKeywords = NormalizeOptional(metaKeywords, MetaKeywordsMaxLength);
        CanonicalUrl = NormalizeOptional(canonicalUrl, CanonicalUrlMaxLength);
    }

    public static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", nameof(slug));
        }

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Contains("..", StringComparison.Ordinal) || normalized.Contains('/', StringComparison.Ordinal) || normalized.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("Slug contains invalid characters.");
        }

        return normalized.Length > SlugMaxLength ? normalized[..SlugMaxLength] : normalized;
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
