using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Themes;

public sealed class ThemeRegistry : IThemeRegistry
{
    private readonly IEnumerable<IThemeProvider> _providers;
    private readonly ILogger<ThemeRegistry> _logger;
    private readonly Lazy<IReadOnlyList<ThemeDescriptor>> _themes;

    public ThemeRegistry(IEnumerable<IThemeProvider> providers, ILogger<ThemeRegistry> logger)
    {
        _providers = providers;
        _logger = logger;
        _themes = new Lazy<IReadOnlyList<ThemeDescriptor>>(LoadThemes, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyList<ThemeDescriptor> GetAll() => _themes.Value;

    public ThemeDescriptor? GetBySystemName(string systemName) =>
        _themes.Value.FirstOrDefault(theme =>
            theme.Manifest.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));

    public ThemeDescriptor GetDefault() =>
        GetBySystemName(DefaultThemeSystemNames.Default)
        ?? _themes.Value.FirstOrDefault()
        ?? throw new InvalidOperationException("No theme providers are registered.");

    private IReadOnlyList<ThemeDescriptor> LoadThemes()
    {
        var list = _providers.Select(provider => provider.GetDescriptor()).ToList();
        if (list.Count == 0)
        {
            _logger.LogWarning("No IThemeProvider implementations were registered.");
        }

        return list;
    }
}

public static class DefaultThemeSystemNames
{
    public const string Default = "Themes.Default";
}

public sealed class ThemeContext(IThemeRegistry registry, string activeThemeSystemName, IReadOnlyDictionary<string, string>? configurationOverrides, string? layoutOverridesJson) : IThemeContext
{
    public string ActiveThemeSystemName { get; } = activeThemeSystemName;

    public ThemeDescriptor ActiveTheme { get; } =
        registry.GetBySystemName(activeThemeSystemName)
        ?? registry.GetDefault();

    public IReadOnlyDictionary<string, string> GetResolvedSettings(IReadOnlyDictionary<string, string>? storeOverrides)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (storeOverrides is not null)
        {
            foreach (var pair in storeOverrides)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        if (configurationOverrides is not null)
        {
            foreach (var pair in configurationOverrides)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return ThemeValueSanitizer.SanitizeSettings(ActiveTheme.Manifest, merged);
    }

    public ThemeLayoutDefinition GetLayout(ThemeLayoutType layoutType, string? layoutOverridesJson)
    {
        var baseLayout = ActiveTheme.Manifest.Layouts.FirstOrDefault(layout => layout.LayoutType == layoutType)
            ?? CreateFallbackLayout(layoutType);

        if (string.IsNullOrWhiteSpace(layoutOverridesJson))
        {
            return baseLayout;
        }

        try
        {
            var overrides = JsonSerializer.Deserialize<Dictionary<string, ThemeLayoutOverride>>(layoutOverridesJson);
            if (overrides is null || !overrides.TryGetValue(layoutType.ToString(), out var layoutOverride))
            {
                return baseLayout;
            }

            var zones = layoutOverride.Zones?.Count > 0 ? layoutOverride.Zones : baseLayout.Zones;
            var showSidebar = layoutOverride.ShowSidebar ?? baseLayout.ShowSidebar;
            return new ThemeLayoutDefinition(layoutType, zones, showSidebar);
        }
        catch (JsonException)
        {
            return baseLayout;
        }
    }

    private static ThemeLayoutDefinition CreateFallbackLayout(ThemeLayoutType layoutType) =>
        layoutType switch
        {
            ThemeLayoutType.Homepage => new(ThemeLayoutType.Homepage,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.HomepageSections, ThemeLayoutZoneNames.Footer], false),
            ThemeLayoutType.Product => new(ThemeLayoutType.Product,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.ProductBefore, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.ProductAfter, ThemeLayoutZoneNames.Footer], true),
            ThemeLayoutType.Category => new(ThemeLayoutType.Category,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.CategoryBefore, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.CategoryAfter, ThemeLayoutZoneNames.Footer], true),
            ThemeLayoutType.Search => new(ThemeLayoutType.Search,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], true),
            ThemeLayoutType.Checkout => new(ThemeLayoutType.Checkout,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], false),
            ThemeLayoutType.Cart => new(ThemeLayoutType.Cart,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], false),
            ThemeLayoutType.Account => new(ThemeLayoutType.Account,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], true),
            _ => new(ThemeLayoutType.CmsPage,
                [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], true)
        };

    private sealed class ThemeLayoutOverride
    {
        public List<string>? Zones { get; set; }

        public bool? ShowSidebar { get; set; }
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommerceThemes(this IServiceCollection services)
    {
        services.AddSingleton<IThemeRegistry, ThemeRegistry>();
        return services;
    }
}
