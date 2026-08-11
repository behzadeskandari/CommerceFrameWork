using Commerce.Framework.Core.Entities;

namespace Commerce.Store.Domain.Entities;

public sealed class Language : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 10;
    public const int CultureMaxLength = 20;

    private Language()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string LanguageCode { get; private set; } = string.Empty;

    public string CultureCode { get; private set; } = string.Empty;

    public string NativeName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsRtl { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Language Create(
        string name,
        string languageCode,
        string cultureCode,
        string nativeName,
        bool isRtl,
        int displayOrder = 0,
        bool isActive = true)
    {
        Validate(name, languageCode, cultureCode);
        var now = DateTime.UtcNow;
        return new Language
        {
            Name = name.Trim(),
            LanguageCode = languageCode.Trim().ToLowerInvariant(),
            CultureCode = cultureCode.Trim(),
            NativeName = string.IsNullOrWhiteSpace(nativeName) ? name.Trim() : nativeName.Trim(),
            IsRtl = isRtl,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(
        string name,
        string cultureCode,
        string nativeName,
        bool isRtl,
        int displayOrder,
        bool isActive)
    {
        Validate(name, LanguageCode, cultureCode);
        Name = name.Trim();
        CultureCode = cultureCode.Trim();
        NativeName = string.IsNullOrWhiteSpace(nativeName) ? name.Trim() : nativeName.Trim();
        IsRtl = isRtl;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void Validate(string name, string languageCode, string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Language code is required.", nameof(languageCode));
        }

        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            throw new ArgumentException("Culture code is required.", nameof(cultureCode));
        }
    }
}
