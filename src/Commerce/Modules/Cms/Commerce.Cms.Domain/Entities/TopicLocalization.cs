using Commerce.Framework.Core.Entities;

namespace Commerce.Cms.Domain.Entities;

public sealed class TopicLocalization : Entity
{
    public const int TitleMaxLength = 500;
    public const int MetaTitleMaxLength = 500;
    public const int MetaDescriptionMaxLength = 1000;

    public int TopicId { get; private set; }

    public int LanguageId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public static TopicLocalization Create(
        int topicId,
        int languageId,
        string title,
        string body,
        string? metaTitle,
        string? metaDescription) =>
        new()
        {
            TopicId = topicId,
            LanguageId = languageId,
            Title = NormalizeRequired(title),
            Body = body ?? string.Empty,
            MetaTitle = NormalizeOptional(metaTitle, MetaTitleMaxLength),
            MetaDescription = NormalizeOptional(metaDescription, MetaDescriptionMaxLength)
        };

    public void Update(string title, string body, string? metaTitle, string? metaDescription)
    {
        Title = NormalizeRequired(title);
        Body = body ?? string.Empty;
        MetaTitle = NormalizeOptional(metaTitle, MetaTitleMaxLength);
        MetaDescription = NormalizeOptional(metaDescription, MetaDescriptionMaxLength);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Title is required.", nameof(value));
        }

        var trimmed = value.Trim();
        return trimmed.Length > TitleMaxLength ? trimmed[..TitleMaxLength] : trimmed;
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
