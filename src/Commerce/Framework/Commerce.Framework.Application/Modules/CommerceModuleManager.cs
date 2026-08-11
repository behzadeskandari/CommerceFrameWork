using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Application.Modules;

public sealed class CommerceModuleManager : ICommerceModuleManager
{
    private readonly ModuleRegistrationContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommerceModuleManager> _logger;
    private readonly object _sync = new();
    private bool _registered;
    private bool _initialized;
    private bool _started;

    public CommerceModuleManager(
        ModuleRegistrationContext context,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CommerceModuleManager> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public IReadOnlyList<ModuleRuntimeInfo> DiscoverModules()
    {
        ValidateModules();
        return _context.OrderedDescriptors
            .Select(descriptor => new ModuleRuntimeInfo(
                descriptor,
                _context.GetEntry(descriptor.SystemName).State,
                _context.GetEntry(descriptor.SystemName).StartupDuration,
                _context.GetEntry(descriptor.SystemName).FailureReason))
            .ToList();
    }

    public void ValidateModules()
    {
        lock (_sync)
        {
            foreach (var entry in _context.Entries.Values)
            {
                if (entry.State is ModuleState.Discovered or ModuleState.Validated)
                {
                    entry.State = ModuleState.Validated;
                }
            }
        }
    }

    public IReadOnlyList<ModuleDescriptor> ResolveDependencies() => _context.OrderedDescriptors;

    public void RegisterModules()
    {
        lock (_sync)
        {
            if (_registered)
            {
                return;
            }

            foreach (var descriptor in _context.OrderedDescriptors)
            {
                var entry = _context.GetEntry(descriptor.SystemName);
                if (entry.State == ModuleState.Disabled)
                {
                    continue;
                }

                entry.State = ModuleState.Registered;
            }

            _registered = true;
        }
    }

    public async Task InitializeModulesAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_initialized)
            {
                return;
            }
        }

        RegisterModules();

        using var scope = _serviceProvider.CreateScope();
        var storeContext = scope.ServiceProvider.GetRequiredService<IStoreContext>();

        foreach (var descriptor in _context.OrderedDescriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = _context.GetEntry(descriptor.SystemName);

            if (entry.State == ModuleState.Disabled)
            {
                continue;
            }

            if (entry.State != ModuleState.Registered)
            {
                throw new InvalidOperationException(
                    $"Module '{descriptor.SystemName}' cannot initialize from state '{entry.State}'.");
            }

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(entry.Module.GetType());

            var context = new CommerceModuleContext(
                descriptor,
                scope.ServiceProvider,
                _configuration,
                storeContext,
                logger);

            try
            {
                _logger.LogInformation("Initializing module {ModuleSystemName} v{ModuleVersion}.", descriptor.SystemName, descriptor.Version);
                await entry.Module.InitializeAsync(context, cancellationToken).ConfigureAwait(false);
                entry.State = ModuleState.Initialized;
            }
            catch (Exception ex)
            {
                entry.State = ModuleState.Failed;
                entry.FailureReason = ex.Message;
                _logger.LogError(ex, "Module {ModuleSystemName} initialization failed.", descriptor.SystemName);

                if (descriptor.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Required module '{descriptor.SystemName}' failed to initialize.",
                        ex);
                }
            }
        }

        lock (_sync)
        {
            _initialized = true;
        }
    }

    public async Task StartModulesAsync(CancellationToken cancellationToken = default)
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
            await InitializeModulesAsync(cancellationToken).ConfigureAwait(false);
        }

        using var scope = _serviceProvider.CreateScope();
        var storeContext = scope.ServiceProvider.GetRequiredService<IStoreContext>();

        foreach (var descriptor in _context.OrderedDescriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = _context.GetEntry(descriptor.SystemName);

            if (entry.State == ModuleState.Disabled)
            {
                continue;
            }

            if (entry.State != ModuleState.Initialized)
            {
                if (entry.State == ModuleState.Failed)
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"Module '{descriptor.SystemName}' cannot start from state '{entry.State}'.");
            }

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(entry.Module.GetType());

            var context = new CommerceModuleContext(
                descriptor,
                scope.ServiceProvider,
                _configuration,
                storeContext,
                logger);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting module {ModuleSystemName}.", descriptor.SystemName);
                await entry.Module.StartAsync(context, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                entry.StartupDuration = stopwatch.Elapsed;
                entry.State = ModuleState.Started;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                entry.StartupDuration = stopwatch.Elapsed;
                entry.State = ModuleState.Failed;
                entry.FailureReason = ex.Message;
                _logger.LogError(ex, "Module {ModuleSystemName} startup failed.", descriptor.SystemName);

                if (descriptor.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Required module '{descriptor.SystemName}' failed to start.",
                        ex);
                }
            }
        }

        lock (_sync)
        {
            _started = true;
        }
    }

    public Task StopModulesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var descriptor in _context.OrderedDescriptors.Reverse())
        {
            var entry = _context.GetEntry(descriptor.SystemName);
            if (entry.State == ModuleState.Started)
            {
                entry.State = ModuleState.Initialized;
            }
        }

        lock (_sync)
        {
            _started = false;
            _initialized = false;
            _registered = false;
        }

        return Task.CompletedTask;
    }
}
