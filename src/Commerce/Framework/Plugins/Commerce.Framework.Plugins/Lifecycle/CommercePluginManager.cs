using Commerce.Framework.PluginContracts.Discovery;
using Commerce.Framework.PluginContracts.Lifecycle;
using Commerce.Framework.PluginContracts.Loading;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.Plugins.Dependency;
using Commerce.Framework.Plugins.Loading;
using Commerce.Framework.PluginContracts.Manifest;
using Commerce.Framework.Plugins.Migrations;
using Commerce.Framework.Plugins.Observability;
using Commerce.Framework.Plugins.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Plugins.Lifecycle;

public sealed class CommercePluginManager : ICommercePluginManager
{
    private readonly PluginRegistrationContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IPluginDiscoveryService _discoveryService;
    private readonly IPluginAssemblyLoader _assemblyLoader;
    private readonly EfPluginRepository _repository;
    private readonly EfPluginStoreConfigurationRepository _storeConfigurationRepository;
    private readonly PluginMigrationRunner _migrationRunner;
    private readonly PluginLifecycleLogger _lifecycleLogger;
    private readonly ILogger<CommercePluginManager> _logger;
    private readonly object _sync = new();
    private bool _registered;
    private bool _initialized;
    private bool _started;

    public CommercePluginManager(
        PluginRegistrationContext context,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IPluginDiscoveryService discoveryService,
        IPluginAssemblyLoader assemblyLoader,
        EfPluginRepository repository,
        EfPluginStoreConfigurationRepository storeConfigurationRepository,
        PluginMigrationRunner migrationRunner,
        PluginLifecycleLogger lifecycleLogger,
        ILogger<CommercePluginManager> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _discoveryService = discoveryService;
        _assemblyLoader = assemblyLoader;
        _repository = repository;
        _storeConfigurationRepository = storeConfigurationRepository;
        _migrationRunner = migrationRunner;
        _lifecycleLogger = lifecycleLogger;
        _logger = logger;
    }

    public IReadOnlyList<PluginRuntimeInfo> Discover()
    {
        Validate();
        return _context.Plugins
            .Select(entry => new PluginRuntimeInfo(
                entry.Descriptor,
                entry.State,
                entry.StartupDuration,
                entry.FailureReason,
                entry.IsInstalled,
                entry.IsEnabled))
            .ToList();
    }

    public void Validate()
    {
        lock (_sync)
        {
            foreach (var entry in _context.Plugins)
            {
                if (entry.State is PluginState.Discovered or PluginState.Invalid or PluginState.Loaded)
                {
                    entry.State = PluginState.Loaded;
                }
            }
        }
    }

    public void Load()
    {
        // Loading occurs during registration context construction and reload operations.
        Validate();
    }

    public void Register()
    {
        lock (_sync)
        {
            if (_registered)
            {
                return;
            }

            foreach (var entry in _context.Plugins.Where(x => x.IsEnabled))
            {
                if (entry.State == PluginState.Disabled)
                {
                    continue;
                }

                entry.State = PluginState.Enabled;
            }

            _registered = true;
        }
    }

    public async Task InstallAsync(string systemName, CancellationToken cancellationToken = default)
    {
        foreach (var descriptor in GetInstallOrder(systemName))
        {
            await InstallSingleAsync(descriptor, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InstallSingleAsync(PluginDescriptor descriptor, CancellationToken cancellationToken)
    {
        var systemName = descriptor.SystemName;
        var existing = await _repository.FindBySystemNameAsync(systemName, cancellationToken).ConfigureAwait(false);
        if (existing is { IsInstalled: true })
        {
            existing.UpdateVersion(descriptor.Version.ToString());
            await _repository.UpdateAsync(cancellationToken).ConfigureAwait(false);
            SyncEntry(descriptor, existing);
            _lifecycleLogger.LogEvent(PluginLifecycleEvents.Updated, systemName, descriptor.Version.ToString());
            return;
        }

        var installation = CommercePluginInstallation.Create(
            descriptor.SystemName,
            descriptor.Version.ToString());

        await _repository.AddAsync(installation, cancellationToken).ConfigureAwait(false);
        SyncEntry(descriptor, installation);

        if (PluginAssemblyRegistry.Instance.Assemblies.TryGetValue(systemName, out var assembly))
        {
            var migrations = PluginMigrationDiscoverer.Discover(assembly, descriptor);
            await _migrationRunner.RunPendingAsync(migrations, cancellationToken).ConfigureAwait(false);
        }

        _lifecycleLogger.LogEvent(PluginLifecycleEvents.Installed, systemName, descriptor.Version.ToString());
    }

    public async Task EnableAsync(string systemName, CancellationToken cancellationToken = default)
    {
        foreach (var descriptor in GetEnableOrder(systemName))
        {
            await EnableSingleAsync(descriptor.SystemName, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnableSingleAsync(string systemName, CancellationToken cancellationToken)
    {
        var installation = await GetInstalledAsync(systemName, cancellationToken).ConfigureAwait(false);
        installation.Enable();
        await _repository.UpdateAsync(cancellationToken).ConfigureAwait(false);

        var entry = _context.GetEntry(systemName);
        entry.IsEnabled = true;
        entry.IsInstalled = true;
        entry.State = PluginState.Enabled;
        _lifecycleLogger.LogEvent(PluginLifecycleEvents.Enabled, systemName, installation.Version);
    }

    public async Task DisableAsync(string systemName, CancellationToken cancellationToken = default)
    {
        foreach (var descriptor in GetDisableOrder(systemName))
        {
            await DisableSingleAsync(descriptor.SystemName, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DisableSingleAsync(string systemName, CancellationToken cancellationToken)
    {
        var installation = await _repository.FindBySystemNameAsync(systemName, cancellationToken).ConfigureAwait(false);
        if (installation is null || !installation.IsInstalled || !installation.IsEnabled)
        {
            return;
        }

        installation.Disable();
        await _repository.UpdateAsync(cancellationToken).ConfigureAwait(false);

        var entry = _context.GetEntry(systemName);
        entry.IsEnabled = false;
        entry.State = PluginState.Disabled;
        _lifecycleLogger.LogEvent(PluginLifecycleEvents.Disabled, systemName, installation.Version);
    }

    public async Task UninstallAsync(
        string systemName,
        PluginUninstallMode uninstallMode = PluginUninstallMode.KeepData,
        CancellationToken cancellationToken = default)
    {
        foreach (var descriptor in GetUninstallOrder(systemName))
        {
            await UninstallSingleAsync(descriptor.SystemName, uninstallMode, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UninstallSingleAsync(
        string systemName,
        PluginUninstallMode uninstallMode,
        CancellationToken cancellationToken)
    {
        var installation = await _repository.FindBySystemNameAsync(systemName, cancellationToken).ConfigureAwait(false);
        if (installation is null)
        {
            return;
        }

        await _repository.RemoveAsync(installation, cancellationToken).ConfigureAwait(false);

        if (uninstallMode == PluginUninstallMode.RemoveData)
        {
            await _storeConfigurationRepository.RemoveForPluginAsync(systemName, cancellationToken).ConfigureAwait(false);
        }

        var entry = _context.GetEntry(systemName);
        entry.IsInstalled = false;
        entry.IsEnabled = false;
        entry.State = PluginState.Loaded;
        _lifecycleLogger.LogEvent(PluginLifecycleEvents.Uninstalled, systemName, installation.Version, message: uninstallMode.ToString());
    }

    public async Task ReloadAsync(string systemName, CancellationToken cancellationToken = default)
    {
        var discovered = _discoveryService.FindBySystemName(systemName)
            ?? throw new InvalidOperationException($"Plugin '{systemName}' was not discovered.");

        _assemblyLoader.Unload(systemName);
        var loaded = _assemblyLoader.Load(discovered.Descriptor);
        var entry = _context.GetEntry(systemName);
        entry.State = PluginState.Loaded;

        var installation = await _repository.FindBySystemNameAsync(systemName, cancellationToken).ConfigureAwait(false);
        if (installation is { IsEnabled: true })
        {
            entry.IsEnabled = true;
            entry.State = PluginState.Enabled;
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_initialized)
            {
                return;
            }
        }

        Register();

        using var scope = _serviceProvider.CreateScope();

        foreach (var entry in _context.Plugins.Where(x => x.IsEnabled))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.State == PluginState.Disabled)
            {
                continue;
            }

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(entry.Plugin.GetType());

            var context = new CommercePluginContext(
                entry.Descriptor,
                scope.ServiceProvider,
                _configuration,
                logger);

            try
            {
                _logger.LogInformation("Initializing plugin {PluginSystemName} v{PluginVersion}.", entry.Descriptor.SystemName, entry.Descriptor.Version);
                await entry.Plugin.InitializeAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                entry.State = PluginState.Failed;
                entry.FailureReason = ex.Message;
                _logger.LogError(ex, "Plugin {PluginSystemName} initialization failed.", entry.Descriptor.SystemName);
                _lifecycleLogger.LogEvent(
                    PluginLifecycleEvents.Failed,
                    entry.Descriptor.SystemName,
                    entry.Descriptor.Version.ToString(),
                    success: false,
                    message: ex.Message);

                var installation = await _repository.FindBySystemNameAsync(entry.Descriptor.SystemName, cancellationToken)
                    .ConfigureAwait(false);
                if (installation is not null)
                {
                    installation.MarkFailed(ex.Message);
                    await _repository.UpdateAsync(cancellationToken).ConfigureAwait(false);
                }

                if (entry.Descriptor.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Required plugin '{entry.Descriptor.SystemName}' failed to initialize.",
                        ex);
                }
            }
        }

        lock (_sync)
        {
            _initialized = true;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_started)
            {
                return;
            }
        }

        if (!_initialized)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        using var scope = _serviceProvider.CreateScope();

        foreach (var entry in _context.Plugins.Where(x => x.IsEnabled))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.State == PluginState.Disabled || entry.State == PluginState.Failed)
            {
                continue;
            }

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(entry.Plugin.GetType());

            var context = new CommercePluginContext(
                entry.Descriptor,
                scope.ServiceProvider,
                _configuration,
                logger);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting plugin {PluginSystemName}.", entry.Descriptor.SystemName);
                await entry.Plugin.StartAsync(context, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                entry.StartupDuration = stopwatch.Elapsed;
                entry.State = PluginState.Enabled;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                entry.StartupDuration = stopwatch.Elapsed;
                entry.State = PluginState.Failed;
                entry.FailureReason = ex.Message;
                _logger.LogError(ex, "Plugin {PluginSystemName} startup failed.", entry.Descriptor.SystemName);

                if (entry.Descriptor.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Required plugin '{entry.Descriptor.SystemName}' failed to start.",
                        ex);
                }
            }
        }

        lock (_sync)
        {
            _started = true;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var scope = _serviceProvider.CreateScope();

        foreach (var entry in _context.Plugins.Where(x => x.IsEnabled).Reverse())
        {
            if (entry.State != PluginState.Enabled)
            {
                continue;
            }

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(entry.Plugin.GetType());

            var context = new CommercePluginContext(
                entry.Descriptor,
                scope.ServiceProvider,
                _configuration,
                logger);

            await entry.Plugin.StopAsync(context, cancellationToken).ConfigureAwait(false);
            entry.State = PluginState.Disabled;
        }

        lock (_sync)
        {
            _started = false;
            _initialized = false;
            _registered = false;
        }
    }

    public async Task SyncInstallationStateAsync(CancellationToken cancellationToken = default)
    {
        var installations = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        var lookup = installations.ToDictionary(x => x.SystemName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _context.Plugins)
        {
            if (!lookup.TryGetValue(entry.Descriptor.SystemName, out var installation))
            {
                continue;
            }

            entry.IsInstalled = installation.IsInstalled;
            entry.IsEnabled = installation.IsEnabled;
            entry.State = installation.IsEnabled
                ? PluginState.Enabled
                : installation.IsInstalled
                    ? PluginState.Installed
                    : PluginState.Loaded;
        }
    }

    private async Task<CommercePluginInstallation> GetInstalledAsync(
        string systemName,
        CancellationToken cancellationToken)
    {
        var installation = await _repository.FindBySystemNameAsync(systemName, cancellationToken).ConfigureAwait(false);
        if (installation is null || !installation.IsInstalled)
        {
            throw new InvalidOperationException($"Plugin '{systemName}' is not installed.");
        }

        return installation;
    }

    private void SyncEntry(PluginDescriptor descriptor, CommercePluginInstallation installation)
    {
        var entry = _context.GetEntry(descriptor.SystemName);
        entry.IsInstalled = installation.IsInstalled;
        entry.IsEnabled = installation.IsEnabled;
        entry.State = installation.IsEnabled ? PluginState.Enabled : PluginState.Installed;
    }

    private IReadOnlyList<PluginDescriptor> GetInstallOrder(string systemName) =>
        ResolveDependencyOrder(systemName, descriptors => PluginDependencyResolver.Resolve(descriptors));

    private IReadOnlyList<PluginDescriptor> GetEnableOrder(string systemName) =>
        ResolveDependencyOrder(systemName, descriptors => PluginDependencyResolver.Resolve(descriptors));

    private IReadOnlyList<PluginDescriptor> GetDisableOrder(string systemName) =>
        ResolveDependencyOrder(systemName, descriptors => PluginDependencyResolver.Resolve(descriptors).Reverse().ToList());

    private IReadOnlyList<PluginDescriptor> GetUninstallOrder(string systemName) =>
        ResolveDependencyOrder(systemName, descriptors => PluginDependencyResolver.Resolve(descriptors).Reverse().ToList());

    private IReadOnlyList<PluginDescriptor> ResolveDependencyOrder(
        string systemName,
        Func<IReadOnlyList<PluginDescriptor>, IReadOnlyList<PluginDescriptor>> orderFunc)
    {
        var discovered = _discoveryService.FindBySystemName(systemName)
            ?? throw new InvalidOperationException($"Plugin '{systemName}' was not discovered.");

        var allDescriptors = _discoveryService.Discover().Select(x => x.Descriptor).ToList();
        var closure = CollectDependencyClosure(discovered.Descriptor, allDescriptors);
        return orderFunc(closure.ToList());
    }

    private static HashSet<PluginDescriptor> CollectDependencyClosure(
        PluginDescriptor root,
        IReadOnlyList<PluginDescriptor> allDescriptors)
    {
        var lookup = allDescriptors.ToDictionary(x => x.SystemName, StringComparer.OrdinalIgnoreCase);
        var closure = new HashSet<PluginDescriptor>();
        var queue = new Queue<PluginDescriptor>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!closure.Add(current))
            {
                continue;
            }

            foreach (var dependency in current.Dependencies)
            {
                if (lookup.TryGetValue(dependency.PluginSystemName, out var dependencyDescriptor))
                {
                    queue.Enqueue(dependencyDescriptor);
                }
            }
        }

        return closure;
    }
}
