using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Manifest;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Commerce.Plugin.Testing;

public sealed class PluginTestContext : ICommercePluginContext
{
    public PluginTestContext(
        PluginDescriptor descriptor,
        IServiceProvider services,
        IConfiguration configuration,
        ILogger? logger = null)
    {
        Descriptor = descriptor;
        Services = services;
        Configuration = configuration;
        Logger = logger ?? NullLogger.Instance;
    }

    public PluginDescriptor Descriptor { get; }

    public IServiceProvider Services { get; }

    public IConfiguration Configuration { get; }

    public ILogger Logger { get; }
}

public sealed class PluginTestHostBuilder
{
    private PluginManifest? _manifest;
    private string? _pluginDirectory;
    private Action<IServiceCollection>? _configureServices;
    private Version _commerceVersion = new(1, 0, 0);

    public PluginTestHostBuilder WithManifest(PluginManifest manifest)
    {
        _manifest = manifest;
        return this;
    }

    public PluginTestHostBuilder WithManifestFile(string manifestPath)
    {
        _manifest = PluginManifestParser.ParseFile(manifestPath);
        _pluginDirectory = Path.GetDirectoryName(manifestPath);
        return this;
    }

    public PluginTestHostBuilder WithPluginDirectory(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory;
        return this;
    }

    public PluginTestHostBuilder WithCommerceVersion(Version commerceVersion)
    {
        _commerceVersion = commerceVersion;
        return this;
    }

    public PluginTestHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices = configure;
        return this;
    }

    public PluginTestHost Build()
    {
        if (_manifest is null)
        {
            throw new InvalidOperationException("A plugin manifest is required.");
        }

        var pluginDirectory = _pluginDirectory ?? Directory.GetCurrentDirectory();
        var errors = PluginManifestValidator.Validate(_manifest, pluginDirectory, _commerceVersion);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        var descriptor = PluginManifestValidator.ToDescriptor(_manifest, pluginDirectory);
        var services = new ServiceCollection();
        _configureServices?.Invoke(services);
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(configuration);
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        return new PluginTestHost(new PluginTestContext(descriptor, provider, configuration));
    }
}

public sealed class PluginTestHost(PluginTestContext context)
{
    public PluginTestContext Context { get; } = context;

    public async Task RunLifecycleAsync(ICommercePlugin plugin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        await plugin.InitializeAsync(Context, cancellationToken).ConfigureAwait(false);
        await plugin.StartAsync(Context, cancellationToken).ConfigureAwait(false);
        await plugin.StopAsync(Context, cancellationToken).ConfigureAwait(false);
    }
}

public static class PluginManifestTestFactory
{
    public static PluginManifest Create(
        string systemName,
        string name,
        string assembly,
        string version = "1.0.0",
        string minimumCommerceVersion = "1.0.0") =>
        new()
        {
            SystemName = systemName,
            Name = name,
            Version = version,
            Assembly = assembly,
            MinimumCommerceVersion = minimumCommerceVersion
        };
}
