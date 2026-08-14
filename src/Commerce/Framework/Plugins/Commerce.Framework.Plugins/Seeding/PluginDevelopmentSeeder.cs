using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Installation;
using Commerce.Framework.PluginContracts.Discovery;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.PluginContracts.Security;
using Commerce.Framework.PluginContracts.Settings;
using Commerce.Framework.Plugins.Configuration;
using Commerce.Framework.Plugins.Discovery;
using Commerce.Framework.Plugins.Loading;
using Commerce.Framework.Plugins.Localization;
using Commerce.Framework.Plugins.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Commerce.Framework.Plugins.Seeding;

public sealed class PluginDevelopmentSeeder(
    IPluginDiscoveryService discoveryService,
    IServiceScopeFactory scopeFactory,
    IOptions<CommercePluginOptions> options) : ICommerceSeeder
{
    public const string EnabledSettingKey = "Commerce:Plugins:SeedDevelopmentData";

    public int Order => 250;

    public string Name => "Plugin Development Data";

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!options.Value.SeedDevelopmentData)
        {
            return;
        }

        var manual = discoveryService.FindBySystemName("Payment.Manual");
        if (manual is null)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EfPluginRepository>();

        var existing = await repository.FindBySystemNameAsync(manual.Descriptor.SystemName, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var installation = CommercePluginInstallation.Create(
                manual.Descriptor.SystemName,
                manual.Descriptor.Version.ToString());
            installation.Enable();
            await repository.AddAsync(installation, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!existing.IsInstalled)
        {
            existing.UpdateVersion(manual.Descriptor.Version.ToString());
        }

        existing.Enable();
        await repository.UpdateAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal static class EnabledPluginBootstrapper
{
    private static readonly HashSet<string> DevelopmentCorePlugins = new(StringComparer.OrdinalIgnoreCase)
    {
        "Payment.Manual",
        "Themes.Default",
        "Search.Database"
    };

    private static readonly HashSet<string> DefaultContextPlugins = new(StringComparer.OrdinalIgnoreCase)
    {
        "Payment.Manual",
        "Themes.Default",
        "Search.Database"
    };

    public static void RegisterEnabledPluginServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var options = configuration.GetSection(CommercePluginOptions.SectionName).Get<CommercePluginOptions>()
            ?? new CommercePluginOptions();

        if (!options.RegisterServicesAtStartup)
        {
            return;
        }

        var discovery = new Discovery.PluginDiscoveryService(hostEnvironment, Options.Create(options));
        var loader = new Loading.PluginAssemblyLoader(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Loading.PluginAssemblyLoader>.Instance);

        var enabledSystemNames = ResolveEnabledSystemNames(configuration, hostEnvironment, options, discovery);
        if (enabledSystemNames.Count == 0)
        {
            return;
        }

        var discovered = discovery.Discover()
            .Where(x => enabledSystemNames.Contains(x.Descriptor.SystemName))
            .ToList();

        if (discovered.Count == 0)
        {
            return;
        }

        var descriptors = discovered.Select(x => x.Descriptor).ToList();
        var orderedDescriptors = Dependency.PluginDependencyResolver.Resolve(descriptors);

        var settingProviders = new List<IPluginSettingDefinitionProvider>();
        var permissionContributors = new List<IPluginPermissionContributor>();
        var localizationCatalog = new PluginLocalizationCatalog();

        foreach (var descriptor in orderedDescriptors)
        {
            if (DefaultContextPlugins.Contains(descriptor.SystemName))
            {
                RegisterFromDefaultContext(
                    services,
                    configuration,
                    descriptor,
                    settingProviders,
                    permissionContributors,
                    localizationCatalog);
                continue;
            }

            var loaded = loader.Load(descriptor);
            loaded.Plugin.RegisterServices(services, configuration);

            PluginAssemblyRegistry.Instance.Register(descriptor.SystemName, loaded.Assembly);
            settingProviders.AddRange(PluginAssemblyScanner.FindSettingProviders(loaded.Assembly));
            permissionContributors.AddRange(PluginAssemblyScanner.FindPermissionContributors(loaded.Assembly));
            PluginLocalizationLoader.LoadFromDirectory(localizationCatalog, descriptor);
        }

        if (settingProviders.Count > 0)
        {
            foreach (var provider in settingProviders)
            {
                services.AddSingleton(provider);
            }

            services.AddSingleton<ISettingDefinitionProvider, Settings.PluginSettingDefinitionAggregator>();
            services.AddSingleton<Settings.PluginSettingSecretRegistry>();
        }

        if (permissionContributors.Count > 0)
        {
            foreach (var contributor in permissionContributors)
            {
                services.AddSingleton(contributor);
            }

            services.AddSingleton<IModulePermissionContributor, Security.PluginDynamicPermissionContributor>();
            services.AddSingleton<Security.PluginPermissionRegistry>();
        }

        services.AddSingleton(localizationCatalog);
    }

    private static HashSet<string> ResolveEnabledSystemNames(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        CommercePluginOptions options,
        Discovery.PluginDiscoveryService discovery)
    {
        var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (TryReadEnabledFromDatabase(hostEnvironment, configuration, enabled))
        {
            return enabled;
        }

        if (options.SeedDevelopmentData)
        {
            foreach (var plugin in discovery.Discover().Where(x => x.Descriptor.IsSystemPlugin))
            {
                if (DevelopmentCorePlugins.Contains(plugin.Descriptor.SystemName))
                {
                    enabled.Add(plugin.Descriptor.SystemName);
                }
            }
        }

        return enabled;
    }

    private static void RegisterFromDefaultContext(
        IServiceCollection services,
        IConfiguration configuration,
        PluginDescriptor descriptor,
        List<IPluginSettingDefinitionProvider> settingProviders,
        List<IPluginPermissionContributor> permissionContributors,
        PluginLocalizationCatalog localizationCatalog)
    {
        var assemblyPath = Path.Combine(descriptor.PluginDirectory, descriptor.AssemblyName);
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Plugin assembly not found at '{assemblyPath}'.", assemblyPath);
        }

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        var pluginType = assembly.ExportedTypes
            .FirstOrDefault(t => typeof(ICommercePlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            ?? throw new InvalidOperationException(
                $"Plugin assembly '{descriptor.AssemblyName}' does not contain an ICommercePlugin implementation.");

        var plugin = (ICommercePlugin)Activator.CreateInstance(pluginType)!;
        plugin.RegisterServices(services, configuration);

        PluginAssemblyRegistry.Instance.Register(descriptor.SystemName, assembly);
        settingProviders.AddRange(PluginAssemblyScanner.FindSettingProviders(assembly));
        permissionContributors.AddRange(PluginAssemblyScanner.FindPermissionContributors(assembly));
        PluginLocalizationLoader.LoadFromDirectory(localizationCatalog, descriptor);
    }

    private static bool TryReadEnabledFromDatabase(
        IHostEnvironment hostEnvironment,
        IConfiguration configuration,
        HashSet<string> enabledSystemNames)
    {
        var connectionFile = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "commerce.database.json");
        if (!File.Exists(connectionFile))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(connectionFile);
            using var document = JsonDocument.Parse(json);
            var connectionString = document.RootElement.TryGetProperty("ConnectionString", out var property)
                ? property.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            var optionsBuilder = new DbContextOptionsBuilder<BootstrapCommerceDbContext>();
            if (string.IsNullOrWhiteSpace(connectionString) ||
                connectionString.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                optionsBuilder.UseInMemoryDatabase(connectionString);
            }
            else
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

            using var dbContext = new BootstrapCommerceDbContext(optionsBuilder.Options);
            var installations = dbContext.Set<CommercePluginInstallation>()
                .AsNoTracking()
                .Where(x => x.IsInstalled && x.IsEnabled)
                .Select(x => x.SystemName)
                .ToList();

            foreach (var systemName in installations)
            {
                enabledSystemNames.Add(systemName);
            }

            return installations.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private sealed class BootstrapCommerceDbContext(DbContextOptions<BootstrapCommerceDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CommercePluginInstallationConfiguration());
        }
    }
}
