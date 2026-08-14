using Commerce.Framework.Core.Entities;

namespace Commerce.Cms.Domain.Entities;

public sealed class ContentPage : AggregateRoot
{
    public const int SystemNameMaxLength = 200;

    public int StoreId { get; private set; }

    public string? SystemName { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTime? PublishedFromUtc { get; private set; }

    public DateTime? PublishedToUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private readonly List<ContentPageLocalization> _localizations = [];
    public IReadOnlyCollection<ContentPageLocalization> Localizations => _localizations;

    public static ContentPage Create(int storeId, string? systemName, bool isPublished, DateTime? publishedFromUtc, DateTime? publishedToUtc)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        ValidateSchedule(publishedFromUtc, publishedToUtc);
        var now = DateTime.UtcNow;
        return new ContentPage
        {
            StoreId = storeId,
            SystemName = NormalizeOptional(systemName, SystemNameMaxLength),
            IsPublished = isPublished,
            PublishedFromUtc = publishedFromUtc,
            PublishedToUtc = publishedToUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string? systemName, bool isPublished, DateTime? publishedFromUtc, DateTime? publishedToUtc)
    {
        ValidateSchedule(publishedFromUtc, publishedToUtc);
        SystemName = NormalizeOptional(systemName, SystemNameMaxLength);
        IsPublished = isPublished;
        PublishedFromUtc = publishedFromUtc;
        PublishedToUtc = publishedToUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsVisible(DateTime utcNow) =>
        IsPublished &&
        (!PublishedFromUtc.HasValue || utcNow >= PublishedFromUtc.Value) &&
        (!PublishedToUtc.HasValue || utcNow <= PublishedToUtc.Value);

    public ContentPageLocalization AddLocalization(
        int languageId,
        string title,
        string slug,
        string body,
        string? metaTitle,
        string? metaDescription,
        string? metaKeywords,
        string? canonicalUrl)
    {
        if (_localizations.Any(x => x.LanguageId == languageId))
        {
            throw new InvalidOperationException("Localization for this language already exists.");
        }

        var localization = ContentPageLocalization.Create(Id, languageId, title, slug, body, metaTitle, metaDescription, metaKeywords, canonicalUrl);
        _localizations.Add(localization);
        UpdatedAtUtc = DateTime.UtcNow;
        return localization;
    }

    public void ReplaceLocalizations(IEnumerable<ContentPageLocalization> localizations)
    {
        _localizations.Clear();
        _localizations.AddRange(localizations);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateSchedule(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ArgumentException("PublishedFrom cannot be after PublishedTo.");
        }
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
