using System.Text.Json;

namespace Commerce.Promotions.Application.Rules;

internal static class PromotionParameterReader
{
    public static decimal ReadDecimal(string json, string propertyName, decimal defaultValue = 0m)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(propertyName, out var value) && value.TryGetDecimal(out var result))
        {
            return result;
        }

        return defaultValue;
    }

    public static int ReadInt(string json, string propertyName, int defaultValue = 0)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result))
        {
            return result;
        }

        return defaultValue;
    }

    public static IReadOnlyList<int> ReadIntList(string json, string propertyName)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty(propertyName, out var value) || value.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(x => x.TryGetInt32(out _))
            .Select(x => x.GetInt32())
            .ToList();
    }

    public static bool ReadBool(string json, string propertyName, bool defaultValue = false)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        return defaultValue;
    }
}
