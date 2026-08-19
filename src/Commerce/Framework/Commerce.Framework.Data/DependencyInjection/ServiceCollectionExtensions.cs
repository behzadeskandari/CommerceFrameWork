using Commerce.Framework.Application.Installation;
using Commerce.Framework.Application.Modules;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Installation;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Data.Migrations.Core;
using Commerce.Framework.Data.Seeding;
using Commerce.Framework.Data.Tenancy;
using Commerce.Framework.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Commerce.Framework.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommerceData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CommerceDataOptions>(configuration.GetSection(CommerceDataOptions.SectionName));
        services.AddSingleton<IInstallationConnectionProvider, FileInstallationConnectionProvider>();
        services.AddSingleton<ICommerceDbContextConfigurator, DynamicCommerceDbContextConfigurator>();
        services.AddSingleton<InstallationRequirementsEvaluator>();

        services.AddCommerceDbContext();

        services.AddSingleton<ICommerceMigration, CoreInitialMigration>();
        services.AddSingleton<MigrationRegistry>(sp =>
        {
            var migrations = sp.GetServices<ICommerceMigration>();
            var moduleContext = sp.GetService<ModuleRegistrationContext>();
            return new MigrationRegistry(migrations, moduleContext?.OrderedSystemNames);
        });
        services.AddScoped<MigrationRunner>();

        services.AddSingleton<ICommerceSeeder, InstallationMetadataSeeder>();
        services.AddSingleton<ICommerceSeeder, DefaultSettingsSeeder>();
        services.AddScoped<SeederRunner>(sp => new SeederRunner(
            sp.GetServices<ICommerceSeeder>(),
            sp,
            sp.GetService<ModuleRegistrationContext>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SeederRunner>>()));

        services.AddScoped<StoreContext>();
        services.AddScoped<IStoreContext>(sp => sp.GetRequiredService<StoreContext>());
        services.AddScoped<IStoreContextAccessor, StoreContextAccessor>();
        services.AddScoped<IStoreContextInitializerService, StoreContextInitializerService>();
        services.AddScoped<IModuleSettings, ModuleSettingsService>();
        //services.AddHostedService<StoreContextInitializer>();

        services.AddScoped<IInstallationStateService, InstallationStateService>();
        services.AddScoped<IInstallationService, InstallationService>();
        services.Configure<CommerceDeploymentOptions>(configuration.GetSection(CommerceDeploymentOptions.SectionName));
        //services.AddHostedService<Deployment.DeploymentStartupHostedService>();

        return services;
    }

    public static async Task LoadPersistedInstallationConfigurationAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var connectionProvider = serviceProvider.GetRequiredService<IInstallationConnectionProvider>();
        await connectionProvider.LoadPersistedAsync(cancellationToken).ConfigureAwait(false);
    }
}
