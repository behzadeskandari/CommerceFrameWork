using Commerce.Framework.Themes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Theme.Default;

public static class DefaultThemeRegistration
{
    public static IServiceCollection AddDefaultTheme(this IServiceCollection services)
    {
        services.AddSingleton<IThemeProvider, DefaultThemeProvider>();
        return services;
    }
}

public sealed class DefaultThemeProvider : IThemeProvider
{
    public ThemeDescriptor GetDescriptor() => DefaultThemeManifest.CreateDescriptor();
}

internal static class DefaultThemeManifest
{
    public static ThemeDescriptor CreateDescriptor()
    {
        var manifest = new ThemeManifest(
            SystemName: DefaultThemeSystemNames.Default,
            Name: "Default Storefront",
            Version: "1.0.0",
            Author: "Commerce",
            Description: "Default ecommerce storefront theme with widget zones and branding settings.",
            Assets: new ThemeAssetReferences(
                Css: ["/themes/default/theme.css"],
                Fonts: []),
            Settings:
            [
                new ThemeSettingDefinition("primaryColor", "Primary color", "color", "#0f766e"),
                new ThemeSettingDefinition("surfaceColor", "Surface color", "color", "#ffffff"),
                new ThemeSettingDefinition("surfaceMutedColor", "Muted surface", "color", "#f9fafb"),
                new ThemeSettingDefinition("textColor", "Text color", "color", "#111827"),
                new ThemeSettingDefinition("textMutedColor", "Muted text", "color", "#6b7280"),
                new ThemeSettingDefinition("headerHeight", "Header height", "size", "64px"),
                new ThemeSettingDefinition("fontFamily", "Font family", "font", "system-ui, sans-serif")
            ],
            Layouts:
            [
                new ThemeLayoutDefinition(ThemeLayoutType.Homepage,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.HomepageSections, ThemeLayoutZoneNames.Footer], false),
                new ThemeLayoutDefinition(ThemeLayoutType.Product,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.ProductBefore, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.ProductAfter, ThemeLayoutZoneNames.Footer], true),
                new ThemeLayoutDefinition(ThemeLayoutType.Category,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.CategoryBefore, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.CategoryAfter, ThemeLayoutZoneNames.Footer], true),
                new ThemeLayoutDefinition(ThemeLayoutType.Search,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], true),
                new ThemeLayoutDefinition(ThemeLayoutType.Cart,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], false),
                new ThemeLayoutDefinition(ThemeLayoutType.Checkout,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], false),
                new ThemeLayoutDefinition(ThemeLayoutType.Account,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], true),
                new ThemeLayoutDefinition(ThemeLayoutType.CmsPage,
                    [ThemeLayoutZoneNames.Header, ThemeLayoutZoneNames.Main, ThemeLayoutZoneNames.Footer], true)
            ]);

        return new ThemeDescriptor(manifest);
    }
}
