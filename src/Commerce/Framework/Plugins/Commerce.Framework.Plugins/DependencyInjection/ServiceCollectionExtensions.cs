using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.PluginContracts.Admin;
using Commerce.Framework.PluginContracts.Discovery;
using Commerce.Framework.PluginContracts.Lifecycle;
using Commerce.Framework.PluginContracts.Loading;
using Commerce.Framework.PluginContracts.Packages;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.Plugins.Configuration;
using Commerce.Framework.Plugins.Dependency;
using Commerce.Framework.Plugins.Lifecycle;
using Commerce.Framework.Plugins.Loading;
using Commerce.Framework.PluginContracts.Manifest;
using Commerce.Framework.Plugins.Persistence;
using Commerce.Framework.Plugins.Security;
using Commerce.Framework.Plugins.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Commerce.Framework.Plugins.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommercePlugins(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.Configure<CommercePluginOptions>(configuration.GetSection(CommercePluginOptions.SectionName));

        services.AddSingleton<IPluginDiscoveryService, Discovery.PluginDiscoveryService>();
        services.AddSingleton<IPluginAssemblyLoader, PluginAssemblyLoader>();
        services.AddSingleton<IPluginPackageService, Packages.PluginPackageService>();
        services.AddScoped<EfPluginRepository>();
        services.AddScoped<EfPluginStoreConfigurationRepository>();
        services.AddScoped<Admin.PluginAdminService>();
        services.AddScoped<IPluginAdminService>(sp => sp.GetRequiredService<Admin.PluginAdminService>());
        services.AddScoped<Migrations.PluginMigrationRunner>();
        services.AddSingleton<Observability.PluginLifecycleLogger>();

        services.AddSingleton<ICommerceModelContributor, PluginModelContributor>();
        services.AddSingleton<ICommerceMigration, Migrations.PluginInitialMigration>();
        services.AddSingleton<ICommerceMigration, Migrations.PluginStoreConfigurationMigration>();
        services.AddSingleton<ICommerceSeeder, PluginDevelopmentSeeder>();
        services.AddSingleton<IModulePermissionContributor, PluginPermissionContributor>();

        services.AddSingleton(CreateRegistrationContext(services, configuration, environment));

        services.AddSingleton<IPluginRegistrationContext>(sp => sp.GetRequiredService<PluginRegistrationContext>());
        services.AddScoped<ICommercePluginManager, CommercePluginManager>();

        return services;
    }

    public static IServiceCollection RegisterEnabledPluginServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        EnabledPluginBootstrapper.RegisterEnabledPluginServices(services, configuration, environment);
        return services;
    }

    public static IServiceCollection AddCommercePluginRuntime(this IServiceCollection services)
    {
        services.AddHostedService<PluginStartupHostedService>();
        return services;
    }

    private static PluginRegistrationContext CreateRegistrationContext(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection(CommercePluginOptions.SectionName).Get<CommercePluginOptions>()
            ?? new CommercePluginOptions();

        var discovery = new Discovery.PluginDiscoveryService(environment, Microsoft.Extensions.Options.Options.Create(options));
        var loader = new PluginAssemblyLoader(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PluginAssemblyLoader>.Instance);

        var discovered = discovery.Discover();
        var descriptors = discovered.Select(x => x.Descriptor).ToList();
        var orderedDescriptors = descriptors.Count == 0
            ? descriptors
            : PluginDependencyResolver.Resolve(descriptors);

        var entries = new List<LoadedPluginEntry>();
        foreach (var descriptor in orderedDescriptors)
        {
            try
            {
                var loaded = loader.Load(descriptor);
                entries.Add(new LoadedPluginEntry(descriptor, loaded.Plugin, PluginState.Loaded));
            }
            catch
            {
                entries.Add(new LoadedPluginEntry(descriptor, new InvalidCommercePlugin(descriptor), PluginState.Invalid));
            }
        }

        return new PluginRegistrationContext(entries, orderedDescriptors);
    }

    private sealed class InvalidCommercePlugin(PluginDescriptor descriptor) : ICommercePlugin
    {
        public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException($"Plugin '{descriptor.SystemName}' is invalid."));

        public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException($"Plugin '{descriptor.SystemName}' is invalid."));

        public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
