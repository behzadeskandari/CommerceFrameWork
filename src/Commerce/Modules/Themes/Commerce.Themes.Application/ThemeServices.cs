using System.Text.Json;
using Commerce.Framework.Themes;
using Commerce.Themes.Application.Abstractions;
using Commerce.Themes.Contracts;
using Commerce.Themes.Contracts.Storefront;
using Commerce.Themes.Contracts.Admin;
using Commerce.Themes.Domain.Entities;

namespace Commerce.Themes.Application;

public sealed class ThemeAdminService(IThemeRegistry registry, IThemeRepository repository) : IThemeAdminService
{
    public Task<IReadOnlyList<ThemeSummaryDto>> ListThemesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var themes = registry.GetAll()
            .Select(theme => new ThemeSummaryDto(
                theme.Manifest.SystemName,
                theme.Manifest.Name,
                theme.Manifest.Version,
                theme.Manifest.Author,
                theme.Manifest.Description,
                theme.IsActive))
            .ToList();

        return Task.FromResult<IReadOnlyList<ThemeSummaryDto>>(themes);
    }

    public Task<ThemeDetailDto?> GetThemeAsync(string systemName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var theme = registry.GetBySystemName(systemName);
        return Task.FromResult(theme is null ? null : MapDetail(theme));
    }

    public async Task<StoreThemeAssignmentDto?> GetStoreAssignmentAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var assignment = await repository.GetByStoreIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
        {
            return null;
        }

        return MapAssignment(assignment, ParseSettings(assignment.ConfigurationJson));
    }

    public async Task<StoreThemeAssignmentDto> SaveStoreAssignmentAsync(int storeId, UpdateStoreThemeAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var theme = registry.GetBySystemName(request.ThemeSystemName)
            ?? throw new InvalidOperationException($"Theme '{request.ThemeSystemName}' is not registered.");

        var sanitized = ThemeValueSanitizer.SanitizeSettings(theme.Manifest, request.Settings);
        var configurationJson = JsonSerializer.Serialize(sanitized);
        var layoutOverridesJson = string.IsNullOrWhiteSpace(request.LayoutOverridesJson) ? "{}" : request.LayoutOverridesJson.Trim();

        var existing = await repository.GetByStoreIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = StoreThemeConfiguration.Create(storeId, request.ThemeSystemName, configurationJson, layoutOverridesJson);
        }
        else
        {
            existing.Update(request.ThemeSystemName, configurationJson, layoutOverridesJson);
        }

        await repository.SaveAsync(existing, cancellationToken).ConfigureAwait(false);
        return MapAssignment(existing, sanitized);
    }

    private static ThemeDetailDto MapDetail(ThemeDescriptor theme) =>
        new(
            theme.Manifest.SystemName,
            theme.Manifest.Name,
            theme.Manifest.Version,
            theme.Manifest.Author,
            theme.Manifest.Description,
            theme.Manifest.Assets.Css,
            theme.Manifest.Assets.Fonts,
            theme.Manifest.Settings.Select(setting => new ThemeSettingDto(
                setting.Key,
                setting.Label,
                setting.Type,
                setting.DefaultValue,
                setting.Description)).ToList(),
            theme.Manifest.Layouts.Select(layout => new ThemeLayoutDto(
                layout.LayoutType.ToString(),
                layout.Zones,
                layout.ShowSidebar)).ToList());

    private static StoreThemeAssignmentDto MapAssignment(StoreThemeConfiguration assignment, IReadOnlyDictionary<string, string> settings) =>
        new(
            assignment.StoreId,
            assignment.ThemeSystemName,
            settings,
            assignment.LayoutOverridesJson,
            assignment.UpdatedAtUtc);

    private static IReadOnlyDictionary<string, string> ParseSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }
}

public sealed class ThemeStorefrontService(IThemeRegistry registry, IThemeRepository repository) : IThemeStorefrontService
{
    public async Task<ThemeRuntimeDto> GetRuntimeAsync(int storeId, bool isRtl, CancellationToken cancellationToken = default)
    {
        var assignment = await repository.GetByStoreIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        var themeSystemName = assignment?.ThemeSystemName ?? DefaultThemeSystemNames.Default;
        var theme = registry.GetBySystemName(themeSystemName) ?? registry.GetDefault();
        var settings = ParseSettings(assignment?.ConfigurationJson);
        var sanitized = ThemeValueSanitizer.SanitizeSettings(theme.Manifest, settings);
        var cssVariables = ThemeCssVariableMapper.ToCssVariables(sanitized);
        var context = new ThemeContext(registry, theme.Manifest.SystemName, null, assignment?.LayoutOverridesJson);

        var layouts = theme.Manifest.Layouts
            .Select(layout => new ThemeLayoutDto(
                layout.LayoutType.ToString(),
                context.GetLayout(layout.LayoutType, assignment?.LayoutOverridesJson).Zones,
                context.GetLayout(layout.LayoutType, assignment?.LayoutOverridesJson).ShowSidebar))
            .ToList();

        return new ThemeRuntimeDto(
            theme.Manifest.SystemName,
            theme.Manifest.Name,
            isRtl ? "rtl" : "ltr",
            cssVariables,
            theme.Manifest.Assets.Css,
            layouts);
    }

    private static IReadOnlyDictionary<string, string> ParseSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }
}
