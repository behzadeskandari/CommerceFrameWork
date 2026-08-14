using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Commerce.Audit.Domain.Enums;
using Commerce.Audit.Domain.Entities;

namespace Commerce.Audit.Application.Security;

public static partial class AuditSanitizer
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "authorization",
        "cvv",
        "cardnumber",
        "card_number",
        "creditcard",
        "privatekey",
        "webhooksecret",
        "merchantid"
    ];

    public static string? SanitizeDetailsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var sanitized = SanitizeElement(document.RootElement);
            return JsonSerializer.Serialize(sanitized);
        }
        catch (JsonException)
        {
            return MaskSensitiveText(json);
        }
    }

    public static IReadOnlyDictionary<string, string?> SanitizeDetails(
        IReadOnlyDictionary<string, string?>? details)
    {
        if (details is null || details.Count == 0)
        {
            return new Dictionary<string, string?>();
        }

        return details.ToDictionary(
            pair => pair.Key,
            pair => IsSensitiveKey(pair.Key) ? "***" : MaskSensitiveText(pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    public static string ComputeEntryHash(
        string previousEntryHash,
        DateTime occurredAtUtc,
        AuditCategory category,
        string action,
        AuditActorType actorType,
        string? actorId,
        string? entityType,
        string? entityId,
        bool success,
        string canonicalPayload)
    {
        var material = string.Join('|',
            previousEntryHash,
            occurredAtUtc.ToString("O"),
            category,
            action,
            actorType,
            actorId ?? string.Empty,
            entityType ?? string.Empty,
            entityId ?? string.Empty,
            success,
            canonicalPayload);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string ComputeEntryHash(AuditEntry entry, string canonicalPayload) =>
        ComputeEntryHash(
            entry.PreviousEntryHash,
            entry.OccurredAtUtc,
            entry.Category,
            entry.Action,
            entry.ActorType,
            entry.ActorId,
            entry.EntityType,
            entry.EntityId,
            entry.Success,
            canonicalPayload);

    public static string BuildCanonicalPayload(string? detailsJson) =>
        detailsJson ?? string.Empty;

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

    private static object? SanitizeElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => IsSensitiveKey(property.Name)
                        ? "***"
                        : SanitizeElement(property.Value),
                    StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeElement).ToArray(),
            JsonValueKind.String => MaskSensitiveText(element.GetString()),
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };

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
