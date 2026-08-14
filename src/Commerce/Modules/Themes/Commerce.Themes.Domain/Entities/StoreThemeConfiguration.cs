using Commerce.Framework.Core.Entities;

namespace Commerce.Themes.Domain.Entities;

public sealed class StoreThemeConfiguration : AggregateRoot
{
    public const int ThemeSystemNameMaxLength = 200;
    public const int JsonMaxLength = 8000;

    public int StoreId { get; private set; }

    public string ThemeSystemName { get; private set; } = string.Empty;

    public string ConfigurationJson { get; private set; } = "{}";

    public string LayoutOverridesJson { get; private set; } = "{}";

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static StoreThemeConfiguration Create(int storeId, string themeSystemName, string? configurationJson, string? layoutOverridesJson)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        var now = DateTime.UtcNow;
        return new StoreThemeConfiguration
        {
            StoreId = storeId,
            ThemeSystemName = NormalizeThemeSystemName(themeSystemName),
            ConfigurationJson = NormalizeJson(configurationJson),
            LayoutOverridesJson = NormalizeJson(layoutOverridesJson),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string themeSystemName, string? configurationJson, string? layoutOverridesJson)
    {
        ThemeSystemName = NormalizeThemeSystemName(themeSystemName);
        ConfigurationJson = NormalizeJson(configurationJson);
        LayoutOverridesJson = NormalizeJson(layoutOverridesJson);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeThemeSystemName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Theme system name is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ThemeSystemNameMaxLength)
        {
            throw new ArgumentException("Theme system name is too long.", nameof(value));
        }

        return trimmed;
    }

    private static string NormalizeJson(string? value)
    {
        var json = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
        if (json.Length > JsonMaxLength)
        {
            throw new ArgumentException("Theme JSON payload is too large.");
        }

        return json;
    }
}
