namespace Commerce.Themes.Contracts.Storefront;

public sealed record ThemeRuntimeDto(
    string ThemeSystemName,
    string ThemeName,
    string Direction,
    IReadOnlyDictionary<string, string> CssVariables,
    IReadOnlyList<string> CssAssets,
    IReadOnlyList<Admin.ThemeLayoutDto> Layouts);
