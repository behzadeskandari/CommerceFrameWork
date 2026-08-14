using System.Text.RegularExpressions;

namespace Commerce.Framework.Themes;

public static partial class ThemeValueSanitizer
{
    [GeneratedRegex(@"^#[0-9A-Fa-f]{3}([0-9A-Fa-f]{3})?$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorPattern();

    [GeneratedRegex(@"^\d+(\.\d+)?(px|rem|em|%)$", RegexOptions.CultureInvariant)]
    private static partial Regex SizePattern();

    [GeneratedRegex(@"^[a-zA-Z0-9\s,'\-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex FontFamilyPattern();

    public static string Sanitize(string key, string value, string settingType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return settingType.ToLowerInvariant() switch
        {
            "color" when HexColorPattern().IsMatch(trimmed) => trimmed,
            "size" when SizePattern().IsMatch(trimmed) => trimmed,
            "font" when FontFamilyPattern().IsMatch(trimmed) && trimmed.Length <= 120 => trimmed,
            "text" when trimmed.Length <= 200 && !ContainsUnsafe(trimmed) => trimmed,
            _ => throw new ArgumentException($"Theme setting '{key}' has an invalid value.", key)
        };
    }

    public static IReadOnlyDictionary<string, string> SanitizeSettings(
        ThemeManifest manifest,
        IReadOnlyDictionary<string, string>? overrides)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var setting in manifest.Settings)
        {
            var value = overrides is not null && overrides.TryGetValue(setting.Key, out var overrideValue)
                ? overrideValue
                : setting.DefaultValue;

            result[setting.Key] = Sanitize(setting.Key, value, setting.Type);
        }

        return result;
    }

    private static bool ContainsUnsafe(string value) =>
        value.Contains('<', StringComparison.Ordinal) ||
        value.Contains('>', StringComparison.Ordinal) ||
        value.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
}

public static class ThemeCssVariableMapper
{
    public static IReadOnlyDictionary<string, string> ToCssVariables(IReadOnlyDictionary<string, string> settings) =>
        settings.ToDictionary(
            pair => pair.Key switch
            {
                "primaryColor" => "--primary",
                "surfaceColor" => "--surface",
                "surfaceMutedColor" => "--surface-muted",
                "textColor" => "--text",
                "textMutedColor" => "--text-muted",
                "headerHeight" => "--header-height",
                "fontFamily" => "--font-family",
                _ => $"--theme-{pair.Key}"
            },
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
}
