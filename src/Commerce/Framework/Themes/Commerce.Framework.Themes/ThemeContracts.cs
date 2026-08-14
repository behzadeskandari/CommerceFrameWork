namespace Commerce.Framework.Themes;

public enum ThemeLayoutType
{
    Homepage = 1,
    Product = 2,
    Category = 3,
    Search = 4,
    Cart = 5,
    Checkout = 6,
    Account = 7,
    CmsPage = 8
}

public static class ThemeLayoutZoneNames
{
    public const string Header = "header";
    public const string Main = "main-content";
    public const string Sidebar = "sidebar";
    public const string Footer = "footer";
    public const string HomepageSections = "homepage";
    public const string ProductBefore = "product-before";
    public const string ProductAfter = "product-after";
    public const string CategoryBefore = "category-before";
    public const string CategoryAfter = "category-after";
}

public sealed record ThemeSettingDefinition(
    string Key,
    string Label,
    string Type,
    string DefaultValue,
    string? Description = null);

public sealed record ThemeLayoutDefinition(
    ThemeLayoutType LayoutType,
    IReadOnlyList<string> Zones,
    bool ShowSidebar);

public sealed record ThemeAssetReferences(
    IReadOnlyList<string> Css,
    IReadOnlyList<string> Fonts);

public sealed record ThemeManifest(
    string SystemName,
    string Name,
    string Version,
    string Author,
    string Description,
    ThemeAssetReferences Assets,
    IReadOnlyList<ThemeSettingDefinition> Settings,
    IReadOnlyList<ThemeLayoutDefinition> Layouts);

public sealed record ThemeDescriptor(ThemeManifest Manifest, bool IsActive = true);

public interface IThemeProvider
{
    ThemeDescriptor GetDescriptor();
}

public interface IThemeRegistry
{
    IReadOnlyList<ThemeDescriptor> GetAll();

    ThemeDescriptor? GetBySystemName(string systemName);

    ThemeDescriptor GetDefault();
}

public interface IThemeContext
{
    string ActiveThemeSystemName { get; }

    ThemeDescriptor ActiveTheme { get; }

    IReadOnlyDictionary<string, string> GetResolvedSettings(IReadOnlyDictionary<string, string>? storeOverrides);

    ThemeLayoutDefinition GetLayout(ThemeLayoutType layoutType, string? layoutOverridesJson);
}
