using System.Text.RegularExpressions;

namespace Commerce.Observability.Application.Logging;

public static partial class LogSanitizer
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password", "secret", "token", "apikey", "api_key", "authorization",
        "cvv", "cardnumber", "card_number", "creditcard", "privatekey", "webhooksecret"
    ];

    public static string? SanitizeValue(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!string.IsNullOrWhiteSpace(key) && IsSensitiveKey(key))
        {
            return "***";
        }

        return MaskSensitiveText(value);
    }

    public static string? MaskSensitiveText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var masked = BearerTokenRegex().Replace(value, "Bearer ***");
        masked = ConnectionPasswordRegex().Replace(masked, "$1=***");
        return masked;
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return SensitiveKeyFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }

    [GeneratedRegex("Bearer\\s+\\S+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex("(Password|Pwd|Secret|Token)\\s*=\\s*[^;\\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionPasswordRegex();
}
