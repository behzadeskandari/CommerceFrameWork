using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.PluginContracts.Admin;
using PluginMigrationStatusDto = Commerce.Framework.PluginContracts.Admin.PluginMigrationStatusDto;
using PluginUiMetadataDto = Commerce.Framework.PluginContracts.Admin.PluginUiMetadataDto;
using Commerce.Framework.PluginContracts.Discovery;
using Commerce.Framework.PluginContracts.Lifecycle;
using Commerce.Framework.PluginContracts.Packages;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.PluginContracts.Security;
using Commerce.Framework.PluginContracts.Settings;
using Commerce.Framework.Plugins.Discovery;
using Commerce.Framework.Plugins.Loading;
using Commerce.Framework.Plugins.Localization;
using Commerce.Framework.Plugins.Migrations;
using Commerce.Framework.Plugins.Observability;
using Commerce.Framework.Plugins.Persistence;
using Commerce.Framework.Plugins.Security;
using Commerce.Framework.Plugins.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Plugins.Admin;

public sealed class PluginAdminService(
    IPluginDiscoveryService discoveryService,
    ICommercePluginManager pluginManager,
    EfPluginRepository repository,
    EfPluginStoreConfigurationRepository storeConfigurationRepository,
    IPluginPackageService packageService,
    PluginMigrationRunner migrationRunner,
    PluginLifecycleLogger lifecycleLogger,
    IServiceProvider serviceProvider,
    IAuditPublisher auditPublisher,
    ILogger<PluginAdminService> logger) : IPluginAdminService
{
    public async Task<Result<IReadOnlyList<PluginSummaryDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var discovered = discoveryService.Discover();
        var installations = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        var installationLookup = installations.ToDictionary(x => x.SystemName, StringComparer.OrdinalIgnoreCase);
        var runtime = pluginManager.Discover().ToDictionary(x => x.Descriptor.SystemName, StringComparer.OrdinalIgnoreCase);

        var summaries = discovered
            .Select(item =>
            {
                installationLookup.TryGetValue(item.Descriptor.SystemName, out var installation);
                runtime.TryGetValue(item.Descriptor.SystemName, out var runtimeInfo);

                return new PluginSummaryDto(
                    item.Descriptor.SystemName,
                    item.Descriptor.Name,
                    item.Descriptor.Version.ToString(),
                    runtimeInfo?.State.ToString() ?? PluginState.Discovered.ToString(),
                    installation?.IsInstalled == true,
                    installation?.IsEnabled == true,
                    item.Descriptor.IsSystemPlugin,
                    item.Descriptor.Author,
                    item.Descriptor.Description);
            })
            .OrderBy(x => x.SystemName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyList<PluginSummaryDto>>(summaries);
    }

    public async Task<Result<PluginDetailDto>> GetAsync(string systemName, CancellationToken cancellationToken = default)
    {
        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Result.Failure<PluginDetailDto>(Error.NotFound($"Plugin '{systemName}' was not found."));
        }

        var installation = await repository.FindBySystemNameAsync(systemName, cancellationToken).ConfigureAwait(false);
        var runtime = pluginManager.Discover()
            .FirstOrDefault(x => string.Equals(x.Descriptor.SystemName, systemName, StringComparison.OrdinalIgnoreCase));

        return Result.Success(new PluginDetailDto(
            discovered.Descriptor.SystemName,
            discovered.Descriptor.Name,
            discovered.Descriptor.Version.ToString(),
            runtime?.State.ToString() ?? PluginState.Discovered.ToString(),
            installation?.IsInstalled == true,
            installation?.IsEnabled == true,
            discovered.Descriptor.IsSystemPlugin,
            discovered.Descriptor.IsRequired,
            discovered.Descriptor.Author,
            discovered.Descriptor.Description,
            discovered.Descriptor.Website,
            discovered.Descriptor.AssemblyName,
            discovered.Descriptor.PluginDirectory,
            discovered.Descriptor.Dependencies
                .Select(d => new PluginDependencyDto(d.PluginSystemName, d.MinimumVersion, d.MaximumVersion))
                .ToList(),
            discovered.Descriptor.MinimumCommerceVersion?.ToString(),
            discovered.Descriptor.MaximumCommerceVersion?.ToString(),
            installation?.LastError,
            installation?.InstalledAt,
            installation?.UpdatedAt,
            true));
    }

    public async Task<Result> InstallAsync(string systemName, CancellationToken cancellationToken = default)
    {
        try
        {
            await pluginManager.InstallAsync(systemName, cancellationToken).ConfigureAwait(false);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Installed, systemName);
            await PublishPluginAuditAsync(AuditActions.PluginInstalled, systemName, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install plugin {PluginSystemName}.", systemName);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Failed, systemName, success: false, message: ex.Message);
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> EnableAsync(string systemName, CancellationToken cancellationToken = default)
    {
        try
        {
            await pluginManager.EnableAsync(systemName, cancellationToken).ConfigureAwait(false);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Enabled, systemName);
            await PublishPluginAuditAsync(AuditActions.PluginEnabled, systemName, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enable plugin {PluginSystemName}.", systemName);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Failed, systemName, success: false, message: ex.Message);
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DisableAsync(string systemName, CancellationToken cancellationToken = default)
    {
        try
        {
            await pluginManager.DisableAsync(systemName, cancellationToken).ConfigureAwait(false);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Disabled, systemName);
            await PublishPluginAuditAsync(AuditActions.PluginDisabled, systemName, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to disable plugin {PluginSystemName}.", systemName);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Failed, systemName, success: false, message: ex.Message);
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> UninstallAsync(
        string systemName,
        PluginUninstallMode uninstallMode = PluginUninstallMode.KeepData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await pluginManager.UninstallAsync(systemName, uninstallMode, cancellationToken).ConfigureAwait(false);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Uninstalled, systemName, message: uninstallMode.ToString());
            await PublishPluginAuditAsync(
                AuditActions.PluginUninstalled,
                systemName,
                cancellationToken,
                new Dictionary<string, string?> { ["uninstallMode"] = uninstallMode.ToString() }).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall plugin {PluginSystemName}.", systemName);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Failed, systemName, success: false, message: ex.Message);
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> ReloadAsync(string systemName, CancellationToken cancellationToken = default)
    {
        try
        {
            await pluginManager.ReloadAsync(systemName, cancellationToken).ConfigureAwait(false);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Updated, systemName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload plugin {PluginSystemName}.", systemName);
            lifecycleLogger.LogEvent(PluginLifecycleEvents.Failed, systemName, success: false, message: ex.Message);
            return Result.Failure(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> InstallFromPackageAsync(Stream packageStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageStream);

        var validation = await packageService.ValidatePackageAsync(packageStream, cancellationToken).ConfigureAwait(false);
        if (validation.IsFailure)
        {
            return Result.Failure(validation.Error!);
        }

        var systemName = validation.Value!.SystemName;
        var discovered = discoveryService.FindBySystemName(systemName);
        var targetDirectory = discovered?.Descriptor.PluginDirectory
            ?? ResolvePackageTargetDirectory(systemName);

        packageStream.Position = 0;
        var extractResult = await packageService.ExtractPackageAsync(packageStream, targetDirectory, cancellationToken)
            .ConfigureAwait(false);

        if (extractResult.IsFailure)
        {
            return Result.Failure(extractResult.Error!);
        }

        return await InstallAsync(systemName, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<IReadOnlyList<PluginSettingEntryDto>>> GetSettingsAsync(
        string systemName,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Result.Failure<IReadOnlyList<PluginSettingEntryDto>>(Error.NotFound($"Plugin '{systemName}' was not found."));
        }

        var settingService = serviceProvider.GetService<ISettingService>();
        if (settingService is null)
        {
            return Result.Success<IReadOnlyList<PluginSettingEntryDto>>(Array.Empty<PluginSettingEntryDto>());
        }

        var providers = GetSettingProviders(systemName);
        var secretRegistry = serviceProvider.GetService<PluginSettingSecretRegistry>();

        var entries = new List<PluginSettingEntryDto>();
        foreach (var definition in providers.SelectMany(x => x.GetDefinitions()))
        {
            var rawValue = await settingService.GetRawAsync(definition.Key, storeId, cancellationToken).ConfigureAwait(false);
            var isSecret = definition.IsSecret || secretRegistry?.IsSecret(definition.Key) == true;
            entries.Add(new PluginSettingEntryDto(
                definition.Key,
                isSecret ? null : rawValue,
                definition.Description,
                definition.ValueType.ToString(),
                definition.IsStoreScoped,
                isSecret,
                !string.IsNullOrWhiteSpace(rawValue)));
        }

        return Result.Success<IReadOnlyList<PluginSettingEntryDto>>(entries);
    }

    public async Task<Result> SaveSettingsAsync(
        string systemName,
        IReadOnlyDictionary<string, string> values,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Result.Failure(Error.NotFound($"Plugin '{systemName}' was not found."));
        }

        var settingService = serviceProvider.GetRequiredService<ISettingService>();
        var allowedKeys = GetSettingProviders(systemName)
            .SelectMany(x => x.GetDefinitions())
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in values)
        {
            if (!allowedKeys.Contains(pair.Key))
            {
                return Result.Failure(Error.Validation($"Setting '{pair.Key}' is not defined for plugin '{systemName}'."));
            }

            await settingService.SetAsync(pair.Key, pair.Value, storeId, cancellationToken).ConfigureAwait(false);
        }

        lifecycleLogger.LogEvent(PluginLifecycleEvents.ConfigurationChanged, systemName);
        await PublishPluginAuditAsync(
            AuditActions.PluginSettingsChanged,
            systemName,
            cancellationToken,
            new Dictionary<string, string?> { ["changedKeys"] = string.Join(",", values.Keys) }).ConfigureAwait(false);
        return Result.Success();
    }

    private Task PublishPluginAuditAsync(
        string action,
        string systemName,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? details = null) =>
        auditPublisher.PublishAsync(new AuditPublishRequest(
            AuditCategory.Plugin,
            action,
            Success: true,
            EntityType: "Plugin",
            EntityId: systemName,
            ActorType: AuditActorType.Administrator,
            Details: details), cancellationToken);

    public Task<Result<IReadOnlyList<PluginPermissionEntryDto>>> GetPermissionsAsync(
        string systemName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<PluginPermissionEntryDto>>(
                Error.NotFound($"Plugin '{systemName}' was not found.")));
        }

        var registry = serviceProvider.GetService<PluginPermissionRegistry>();
        var permissions = registry?.GetPermissions(systemName) ?? GetPermissionContributors(systemName)
            .SelectMany(x => x.GetPermissions())
            .ToList();

        var entries = permissions
            .Select(permission => new PluginPermissionEntryDto(permission.Key, permission.Description))
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<PluginPermissionEntryDto>>(entries));
    }

    public async Task<Result<IReadOnlyList<PluginStoreConfigurationDto>>> GetStoreConfigurationsAsync(
        string systemName,
        CancellationToken cancellationToken = default)
    {
        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Result.Failure<IReadOnlyList<PluginStoreConfigurationDto>>(Error.NotFound($"Plugin '{systemName}' was not found."));
        }

        var configurations = await storeConfigurationRepository.ListForPluginAsync(systemName, cancellationToken)
            .ConfigureAwait(false);

        var dtos = configurations
            .Select(x => new PluginStoreConfigurationDto(x.StoreId, x.IsEnabled, x.ConfigurationJson))
            .ToList();

        return Result.Success<IReadOnlyList<PluginStoreConfigurationDto>>(dtos);
    }

    public async Task<Result> SaveStoreConfigurationAsync(
        string systemName,
        PluginStoreConfigurationDto configuration,
        CancellationToken cancellationToken = default)
    {
        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Result.Failure(Error.NotFound($"Plugin '{systemName}' was not found."));
        }

        var entity = CommercePluginStoreConfiguration.Create(systemName, configuration.StoreId, configuration.IsEnabled);
        entity.SetConfiguration(configuration.ConfigurationJson);
        await storeConfigurationRepository.UpsertAsync(entity, cancellationToken).ConfigureAwait(false);
        lifecycleLogger.LogEvent(PluginLifecycleEvents.ConfigurationChanged, systemName, message: $"Store {configuration.StoreId}");
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<PluginMigrationStatusDto>>> GetMigrationStatusAsync(
        string systemName,
        CancellationToken cancellationToken = default)
    {
        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Result.Failure<IReadOnlyList<PluginMigrationStatusDto>>(Error.NotFound($"Plugin '{systemName}' was not found."));
        }

        if (!PluginAssemblyRegistry.Instance.Assemblies.TryGetValue(systemName, out var assembly))
        {
            var fallbackDiscovered = discoveryService.FindBySystemName(systemName);
            if (fallbackDiscovered is null)
            {
                return Result.Success<IReadOnlyList<PluginMigrationStatusDto>>(Array.Empty<PluginMigrationStatusDto>());
            }

            assembly = PluginReflectionHelper.TryLoadReadOnlyAssembly(fallbackDiscovered.Descriptor);
            if (assembly is null)
            {
                return Result.Success<IReadOnlyList<PluginMigrationStatusDto>>(Array.Empty<PluginMigrationStatusDto>());
            }
        }

        var migrations = PluginMigrationDiscoverer.Discover(assembly, discovered.Descriptor);
        var status = await migrationRunner.GetStatusAsync(systemName, migrations, cancellationToken).ConfigureAwait(false);
        var dtos = status
            .Select(x => new PluginMigrationStatusDto(x.Name, x.Version, x.Description, x.IsApplied))
            .ToList();

        return Result.Success<IReadOnlyList<PluginMigrationStatusDto>>(dtos);
    }

    public Task<Result<PluginUiMetadataDto>> GetUiMetadataAsync(
        string systemName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Task.FromResult(Result.Failure<PluginUiMetadataDto>(Error.NotFound($"Plugin '{systemName}' was not found.")));
        }

        if (!PluginAssemblyRegistry.Instance.Assemblies.TryGetValue(systemName, out var assembly))
        {
            assembly = PluginReflectionHelper.TryLoadReadOnlyAssembly(discovered.Descriptor);
            if (assembly is null)
            {
                return Task.FromResult(Result.Success(new PluginUiMetadataDto([], [])));
            }
        }

        var metadataProviders = PluginAssemblyScanner.FindUiMetadataProviders(assembly);
        var navItems = metadataProviders
            .SelectMany(x => x.GetMetadata().AdminNavItems)
            .Select(item => new PluginAdminNavItemDto(item.Title, item.Route, item.Icon, item.DisplayOrder, item.Permission))
            .ToList();

        var contributions = metadataProviders
            .SelectMany(x => x.GetMetadata().Contributions)
            .Select(item => new PluginUiContributionDto(
                item.Target,
                item.Title,
                item.Permission,
                item.ConfigurationComponent,
                item.DisplayOrder))
            .ToList();

        return Task.FromResult(Result.Success(new PluginUiMetadataDto(navItems, contributions)));
    }

    public Task<Result<IReadOnlyDictionary<string, string>>> GetLocalizationAsync(
        string systemName,
        string culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Task.FromResult(Result.Failure<IReadOnlyDictionary<string, string>>(
                Error.NotFound($"Plugin '{systemName}' was not found.")));
        }

        var catalog = serviceProvider.GetService<PluginLocalizationCatalog>();
        if (catalog is null)
        {
            PluginLocalizationLoader.LoadFromDirectory(new PluginLocalizationCatalog(), discovered.Descriptor);
            catalog = new PluginLocalizationCatalog();
            PluginLocalizationLoader.LoadFromDirectory(catalog, discovered.Descriptor);
        }

        var translations = catalog.GetTranslations(systemName, culture);
        return Task.FromResult(Result.Success(translations));
    }

    private string ResolvePackageTargetDirectory(string systemName)
    {
        var discovered = discoveryService.Discover();
        if (discovered.Count > 0)
        {
            var sampleDirectory = discovered[0].Descriptor.PluginDirectory;
            var rootDirectory = Path.GetDirectoryName(sampleDirectory)
                ?? throw new InvalidOperationException("Unable to resolve plugin root directory.");
            return Path.Combine(rootDirectory, systemName);
        }

        return Path.Combine("Plugins", systemName);
    }

    private IReadOnlyList<IPluginSettingDefinitionProvider> GetSettingProviders(string systemName)
    {
        var registered = serviceProvider.GetServices<IPluginSettingDefinitionProvider>()
            .Where(x => string.Equals(x.PluginSystemName, systemName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (registered.Count > 0)
        {
            return registered;
        }

        var discovered = discoveryService.FindBySystemName(systemName);
        if (discovered is null)
        {
            return Array.Empty<IPluginSettingDefinitionProvider>();
        }

        var assembly = PluginReflectionHelper.TryLoadReadOnlyAssembly(discovered.Descriptor);
        return assembly is null
            ? Array.Empty<IPluginSettingDefinitionProvider>()
            : PluginAssemblyScanner.FindSettingProviders(assembly);
    }

    private IReadOnlyList<IPluginPermissionContributor> GetPermissionContributors(string systemName)
    {
        if (!PluginAssemblyRegistry.Instance.Assemblies.TryGetValue(systemName, out var assembly))
        {
            var discovered = discoveryService.FindBySystemName(systemName);
            if (discovered is null)
            {
                return Array.Empty<IPluginPermissionContributor>();
            }

            assembly = PluginReflectionHelper.TryLoadReadOnlyAssembly(discovered.Descriptor);
            if (assembly is null)
            {
                return Array.Empty<IPluginPermissionContributor>();
            }
        }

        return PluginAssemblyScanner.FindPermissionContributors(assembly);
    }
}
