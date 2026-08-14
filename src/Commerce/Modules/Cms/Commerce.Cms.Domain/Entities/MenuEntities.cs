using Commerce.Cms.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Cms.Domain.Entities;

public sealed class Menu : AggregateRoot
{
    public const int SystemNameMaxLength = 100;
    public const int NameMaxLength = 200;

    public int StoreId { get; private set; }

    public string SystemName { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private readonly List<MenuItem> _items = [];
    public IReadOnlyCollection<MenuItem> Items => _items;

    public static Menu Create(int storeId, string systemName, string name, bool isPublished = true)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        var now = DateTime.UtcNow;
        return new Menu
        {
            StoreId = storeId,
            SystemName = NormalizeSystemName(systemName),
            Name = NormalizeName(name),
            IsPublished = isPublished,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string systemName, string name, bool isPublished)
    {
        SystemName = NormalizeSystemName(systemName);
        Name = NormalizeName(name);
        IsPublished = isPublished;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public MenuItem AddItem(
        int? parentMenuItemId,
        MenuItemLinkType linkType,
        string? url,
        int? contentPageId,
        int? topicId,
        string? externalSlug,
        int displayOrder,
        bool openInNewTab)
    {
        var item = MenuItem.Create(Id, parentMenuItemId, linkType, url, contentPageId, topicId, externalSlug, displayOrder, openInNewTab);
        _items.Add(item);
        UpdatedAtUtc = DateTime.UtcNow;
        return item;
    }

    public void ReplaceItems(IEnumerable<MenuItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeSystemName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("System name is required.", nameof(value));
        }

        var trimmed = value.Trim().ToLowerInvariant();
        return trimmed.Length > SystemNameMaxLength ? trimmed[..SystemNameMaxLength] : trimmed;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Name is required.", nameof(value));
        }

        var trimmed = value.Trim();
        return trimmed.Length > NameMaxLength ? trimmed[..NameMaxLength] : trimmed;
    }
}

public sealed class MenuItem : Entity
{
    public const int UrlMaxLength = 500;
    public const int ExternalSlugMaxLength = 200;

    public int MenuId { get; private set; }

    public int? ParentMenuItemId { get; private set; }

    public MenuItemLinkType LinkType { get; private set; }

    public string? Url { get; private set; }

    public int? ContentPageId { get; private set; }

    public int? TopicId { get; private set; }

    public string? ExternalSlug { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool OpenInNewTab { get; private set; }

    private readonly List<MenuItemLocalization> _localizations = [];
    public IReadOnlyCollection<MenuItemLocalization> Localizations => _localizations;

    public static MenuItem Create(
        int menuId,
        int? parentMenuItemId,
        MenuItemLinkType linkType,
        string? url,
        int? contentPageId,
        int? topicId,
        string? externalSlug,
        int displayOrder,
        bool openInNewTab) =>
        new()
        {
            MenuId = menuId,
            ParentMenuItemId = parentMenuItemId,
            LinkType = linkType,
            Url = NormalizeOptional(url, UrlMaxLength),
            ContentPageId = contentPageId,
            TopicId = topicId,
            ExternalSlug = NormalizeOptional(externalSlug, ExternalSlugMaxLength),
            DisplayOrder = displayOrder,
            OpenInNewTab = openInNewTab
        };

    public MenuItemLocalization AddLocalization(int languageId, string title)
    {
        if (_localizations.Any(x => x.LanguageId == languageId))
        {
            throw new InvalidOperationException("Localization already exists.");
        }

        var localization = MenuItemLocalization.Create(Id, languageId, title);
        _localizations.Add(localization);
        return localization;
    }

    internal void ReplaceLocalizations(IEnumerable<MenuItemLocalization> localizations)
    {
        _localizations.Clear();
        _localizations.AddRange(localizations);
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

public sealed class MenuItemLocalization : Entity
{
    public const int TitleMaxLength = 200;

    public int MenuItemId { get; private set; }

    public int LanguageId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public static MenuItemLocalization Create(int menuItemId, int languageId, string title) =>
        new()
        {
            MenuItemId = menuItemId,
            LanguageId = languageId,
            Title = NormalizeTitle(title)
        };

    public void Update(string title) => Title = NormalizeTitle(title);

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var trimmed = title.Trim();
        return trimmed.Length > TitleMaxLength ? trimmed[..TitleMaxLength] : trimmed;
    }
}
