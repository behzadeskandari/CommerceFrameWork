using Commerce.Framework.Themes;
using Commerce.Themes.Contracts.Storefront;

namespace Commerce.Themes.Contracts.Admin;

public sealed record ThemeSummaryDto(
    string SystemName,
    string Name,
    string Version,
    string Author,
    string Description,
    bool IsRegistered);

public sealed record ThemeSettingDto(
    string Key,
    string Label,
    string Type,
    string DefaultValue,
    string? Description);

public sealed record ThemeLayoutDto(
    string LayoutType,
    IReadOnlyList<string> Zones,
    bool ShowSidebar);

public sealed record ThemeDetailDto(
    string SystemName,
    string Name,
    string Version,
    string Author,
    string Description,
    IReadOnlyList<string> CssAssets,
    IReadOnlyList<string> FontAssets,
    IReadOnlyList<ThemeSettingDto> Settings,
    IReadOnlyList<ThemeLayoutDto> Layouts);

public sealed record StoreThemeAssignmentDto(
    int StoreId,
    string ThemeSystemName,
    IReadOnlyDictionary<string, string> Settings,
    string LayoutOverridesJson,
    DateTime UpdatedAtUtc);

public sealed record UpdateStoreThemeAssignmentRequest(
    string ThemeSystemName,
    IReadOnlyDictionary<string, string>? Settings,
    string? LayoutOverridesJson);

public interface IThemeAdminService
{
    Task<IReadOnlyList<ThemeSummaryDto>> ListThemesAsync(CancellationToken cancellationToken = default);

    Task<ThemeDetailDto?> GetThemeAsync(string systemName, CancellationToken cancellationToken = default);

    Task<StoreThemeAssignmentDto?> GetStoreAssignmentAsync(int storeId, CancellationToken cancellationToken = default);

    Task<StoreThemeAssignmentDto> SaveStoreAssignmentAsync(int storeId, UpdateStoreThemeAssignmentRequest request, CancellationToken cancellationToken = default);
}
