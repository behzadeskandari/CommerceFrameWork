using System.Text.Json;

namespace Commerce.Framework.Scheduling;

public static class JobObservabilityPayload
{
    private const string CorrelationProperty = "__correlationId";

    public static string? ExtractCorrelationId(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(CorrelationProperty, out var correlationElement) &&
                correlationElement.ValueKind == JsonValueKind.String)
            {
                return correlationElement.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    public static string EnrichPayload(string? payloadJson, string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return payloadJson ?? "{}";
        }

        var root = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(payloadJson))
        {
            try
            {
                using var document = JsonDocument.Parse(payloadJson);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        root[property.Name] = property.Value.Clone();
                    }
                }
            }
            catch (JsonException)
            {
                root["payload"] = payloadJson;
            }
        }

        root[CorrelationProperty] = correlationId;
        return JsonSerializer.Serialize(root);
    }
}
