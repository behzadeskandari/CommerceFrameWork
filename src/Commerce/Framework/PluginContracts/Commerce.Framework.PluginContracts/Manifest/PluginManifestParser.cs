using System.Text.Json;
using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.PluginContracts.Manifest;

public static class PluginManifestParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static PluginManifest Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Plugin manifest is empty.");

        return manifest;
    }

    public static PluginManifest ParseFile(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Plugin manifest not found at '{manifestPath}'.", manifestPath);
        }

        return Parse(File.ReadAllText(manifestPath));
    }
}
