using Commerce.Framework.Core.Results;
using Commerce.Framework.PluginContracts.Lifecycle;

namespace Commerce.Framework.PluginContracts.Admin;

public interface IPluginAdminService
{
    Task<Result<IReadOnlyList<PluginSummaryDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<PluginDetailDto>> GetAsync(string systemName, CancellationToken cancellationToken = default);

    Task<Result> InstallAsync(string systemName, CancellationToken cancellationToken = default);

    Task<Result> EnableAsync(string systemName, CancellationToken cancellationToken = default);

    Task<Result> DisableAsync(string systemName, CancellationToken cancellationToken = default);

    Task<Result> UninstallAsync(
        string systemName,
        PluginUninstallMode uninstallMode = PluginUninstallMode.KeepData,
        CancellationToken cancellationToken = default);

    Task<Result> ReloadAsync(string systemName, CancellationToken cancellationToken = default);

    Task<Result> InstallFromPackageAsync(Stream packageStream, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PluginSettingEntryDto>>> GetSettingsAsync(
        string systemName,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    Task<Result> SaveSettingsAsync(
        string systemName,
        IReadOnlyDictionary<string, string> values,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PluginPermissionEntryDto>>> GetPermissionsAsync(
        string systemName,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PluginStoreConfigurationDto>>> GetStoreConfigurationsAsync(
        string systemName,
        CancellationToken cancellationToken = default);

    Task<Result> SaveStoreConfigurationAsync(
        string systemName,
        PluginStoreConfigurationDto configuration,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PluginMigrationStatusDto>>> GetMigrationStatusAsync(
        string systemName,
        CancellationToken cancellationToken = default);

    Task<Result<PluginUiMetadataDto>> GetUiMetadataAsync(
        string systemName,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyDictionary<string, string>>> GetLocalizationAsync(
        string systemName,
        string culture,
        CancellationToken cancellationToken = default);
}

public sealed record PluginSummaryDto(
    string SystemName,
    string Name,
    string Version,
    string State,
    bool IsInstalled,
    bool IsEnabled,
    bool IsSystemPlugin,
    string? Author,
    string? Description);

public sealed record PluginDetailDto(
    string SystemName,
    string Name,
    string Version,
    string State,
    bool IsInstalled,
    bool IsEnabled,
    bool IsSystemPlugin,
    bool IsRequired,
    string? Author,
    string? Description,
    string? Website,
    string AssemblyName,
    string PluginDirectory,
    IReadOnlyList<PluginDependencyDto> Dependencies,
    string? MinimumCommerceVersion,
    string? MaximumCommerceVersion,
    string? LastError,
    DateTimeOffset? InstalledAt,
    DateTimeOffset? UpdatedAt,
    bool RequiresRestartForServiceChanges);

public sealed record PluginDependencyDto(
    string SystemName,
    string? MinimumVersion,
    string? MaximumVersion);

public sealed record PluginSettingEntryDto(
    string Key,
    string? Value,
    string Description,
    string ValueType,
    bool IsStoreScoped,
    bool IsSecret,
    bool HasValue);

public sealed record PluginPermissionEntryDto(
    string Key,
    string Description);

public sealed record PluginStoreConfigurationDto(
    int StoreId,
    bool IsEnabled,
    string? ConfigurationJson);

public sealed record PluginMigrationStatusDto(
    string Name,
    string Version,
    string Description,
    bool IsApplied);

public sealed record PluginUiMetadataDto(
    IReadOnlyList<PluginAdminNavItemDto> AdminNavItems,
    IReadOnlyList<PluginUiContributionDto> Contributions);

public sealed record PluginAdminNavItemDto(
    string Title,
    string Route,
    string? Icon,
    int DisplayOrder,
    string? Permission);

public sealed record PluginUiContributionDto(
    string Target,
    string Title,
    string? Permission,
    string? ConfigurationComponent,
    int DisplayOrder);
