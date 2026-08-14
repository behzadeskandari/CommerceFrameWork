using System.Text.Json;
using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.Plugins.Localization;

public sealed class PluginLocalizationCatalog
{
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _resources = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string pluginSystemName, string culture, IReadOnlyDictionary<string, string> entries)
    {
        if (!_resources.TryGetValue(pluginSystemName, out var cultures))
        {
            cultures = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _resources[pluginSystemName] = cultures;
        }

        cultures[culture] = new Dictionary<string, string>(entries, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> GetTranslations(string pluginSystemName, string culture)
    {
        if (!_resources.TryGetValue(pluginSystemName, out var cultures))
        {
            return new Dictionary<string, string>();
        }

        if (cultures.TryGetValue(culture, out var exact))
        {
            return exact;
        }

        var fallbackCulture = culture.Split('-')[0];
        return cultures.TryGetValue(fallbackCulture, out var fallback)
            ? fallback
            : new Dictionary<string, string>();
    }

    public IReadOnlyList<string> GetSupportedCultures(string pluginSystemName) =>
        _resources.TryGetValue(pluginSystemName, out var cultures)
            ? cultures.Keys.ToList()
            : Array.Empty<string>();
}

public static class PluginLocalizationLoader
{
    public static void LoadFromDirectory(PluginLocalizationCatalog catalog, PluginDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(descriptor);

        var localizationDirectory = Path.Combine(descriptor.PluginDirectory, "Localization");
        if (!Directory.Exists(localizationDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(localizationDirectory, "*.json"))
        {
            var culture = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(culture))
            {
                continue;
            }

            var json = File.ReadAllText(file);
            var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();

            var namespaced = entries.ToDictionary(
                pair => pair.Key.StartsWith($"{descriptor.SystemName}.", StringComparison.OrdinalIgnoreCase)
                    ? pair.Key
                    : $"{descriptor.SystemName}.{pair.Key}",
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

            catalog.Register(descriptor.SystemName, culture, namespaced);
        }
    }
}
