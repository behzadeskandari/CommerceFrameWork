using System.Text.Json;

namespace Commerce.Framework.Infrastructure.Caching;

internal static class CacheSerialization
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static byte[] Serialize<T>(T value) where T : class =>
        JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

    public static T? Deserialize<T>(byte[] payload) where T : class =>
        JsonSerializer.Deserialize<T>(payload, SerializerOptions);
}
