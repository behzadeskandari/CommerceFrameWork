using Commerce.Framework.Core.Entities;

namespace Commerce.Cms.Domain.Entities;

public sealed class Topic : AggregateRoot
{
    public const int SystemNameMaxLength = 200;

    public int StoreId { get; private set; }

    public string SystemName { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; }

    public DateTime? PublishedFromUtc { get; private set; }

    public DateTime? PublishedToUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private readonly List<TopicLocalization> _localizations = [];
    public IReadOnlyCollection<TopicLocalization> Localizations => _localizations;

    public static Topic Create(int storeId, string systemName, bool isPublished, DateTime? publishedFromUtc, DateTime? publishedToUtc)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        var now = DateTime.UtcNow;
        return new Topic
        {
            StoreId = storeId,
            SystemName = NormalizeSystemName(systemName),
            IsPublished = isPublished,
            PublishedFromUtc = publishedFromUtc,
            PublishedToUtc = publishedToUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string systemName, bool isPublished, DateTime? publishedFromUtc, DateTime? publishedToUtc)
    {
        SystemName = NormalizeSystemName(systemName);
        IsPublished = isPublished;
        PublishedFromUtc = publishedFromUtc;
        PublishedToUtc = publishedToUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsVisible(DateTime utcNow) =>
        IsPublished &&
        (!PublishedFromUtc.HasValue || utcNow >= PublishedFromUtc.Value) &&
        (!PublishedToUtc.HasValue || utcNow <= PublishedToUtc.Value);

    public TopicLocalization AddLocalization(int languageId, string title, string body, string? metaTitle, string? metaDescription)
    {
        if (_localizations.Any(x => x.LanguageId == languageId))
        {
            throw new InvalidOperationException("Localization for this language already exists.");
        }

        var localization = TopicLocalization.Create(Id, languageId, title, body, metaTitle, metaDescription);
        _localizations.Add(localization);
        UpdatedAtUtc = DateTime.UtcNow;
        return localization;
    }

    public void ReplaceLocalizations(IEnumerable<TopicLocalization> localizations)
    {
        _localizations.Clear();
        _localizations.AddRange(localizations);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeSystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }

        var trimmed = systemName.Trim().ToLowerInvariant();
        return trimmed.Length > SystemNameMaxLength ? trimmed[..SystemNameMaxLength] : trimmed;
    }
}
