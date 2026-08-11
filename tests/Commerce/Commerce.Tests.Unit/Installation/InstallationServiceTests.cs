using Commerce.Framework.Application.Installation;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Installation;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Data.Migrations.Core;
using Commerce.Framework.Data.Seeding;
using Commerce.Framework.Data.Tenancy;
using Commerce.Framework.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Commerce.Tests.Unit.Installation;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashPassword_DoesNotStorePlainText()
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.HashPassword("Password123!");

        Assert.DoesNotContain("Password123!", hash);
        Assert.True(hasher.VerifyPassword(hash, "Password123!"));
    }
}

public sealed class InstallationRequirementsEvaluatorTests
{
    [Fact]
    public void Evaluate_WithValidConfiguration_ReturnsSatisfiedChecks()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Commerce:ApplicationName"] = "Commerce"
            })
            .Build();

        var evaluator = new InstallationRequirementsEvaluator();
        var results = evaluator.Evaluate(configuration, Path.GetTempPath());

        Assert.Contains(results, x => x.Name == "Runtime" && x.IsSatisfied);
        Assert.Contains(results, x => x.Name == "ApplicationConfiguration" && x.IsSatisfied);
    }
}

public sealed class InstallationServiceTests
{
    [Fact]
    public async Task CompleteInstallation_CreatesInstalledState()
    {
        var (provider, inMemoryToken) = BuildServiceProvider();
        await using (provider)
        {
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IInstallationService>();

            var database = await service.ConfigureDatabaseAsync(new DatabaseSetupRequest(
                "SqlServer",
                inMemoryToken));

            Assert.True(database.IsSuccess);

            using var migrateScope = provider.CreateScope();
            var migrateResult = await migrateScope.ServiceProvider.GetRequiredService<IInstallationService>()
                .RunMigrationsAsync();
            Assert.True(migrateResult.IsSuccess);

            using var seedScope = provider.CreateScope();
            Assert.True((await seedScope.ServiceProvider.GetRequiredService<IInstallationService>().RunSeedAsync()).IsSuccess);

            using var adminScope = provider.CreateScope();
            Assert.True((await adminScope.ServiceProvider.GetRequiredService<IInstallationService>().CreateAdministratorAsync(
                new AdministratorSetupRequest("admin@test.com", "admin", "Password123!"))).IsSuccess);

            using var storeScope = provider.CreateScope();
            Assert.True((await storeScope.ServiceProvider.GetRequiredService<IInstallationService>().CreateStoreAsync(
                new StoreSetupRequest("Store", "https://store.test", "store.test"))).IsSuccess);

            using var languageScope = provider.CreateScope();
            Assert.True((await languageScope.ServiceProvider.GetRequiredService<IInstallationService>().ConfigureLanguageAsync(
                new LanguageSetupRequest("English", "en-US", false, true))).IsSuccess);

            using var currencyScope = provider.CreateScope();
            Assert.True((await currencyScope.ServiceProvider.GetRequiredService<IInstallationService>().ConfigureCurrencyAsync(
                new CurrencySetupRequest("USD", "USD", 1m, true))).IsSuccess);

            using var completeScope = provider.CreateScope();
            Assert.True((await completeScope.ServiceProvider.GetRequiredService<IInstallationService>().CompleteInstallationAsync()).IsSuccess);

            using var verifyScope = provider.CreateScope();
            var state = await verifyScope.ServiceProvider.GetRequiredService<IInstallationStateService>().GetStateAsync();
            Assert.Equal(InstallationStatus.Installed, state.Status);
            Assert.True(state.IsLocked);
        }
    }

    [Fact]
    public async Task CreateAdministrator_RejectsPlaintextPersistence()
    {
        var (provider, inMemoryToken) = BuildServiceProvider();
        await using (provider)
        {
            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IInstallationService>();
                await service.ConfigureDatabaseAsync(new DatabaseSetupRequest("SqlServer", inMemoryToken));
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IInstallationService>();
                await service.RunMigrationsAsync();
                await service.RunSeedAsync();
                await service.CreateAdministratorAsync(new AdministratorSetupRequest("a@test.com", "admin", "Password123!"));
            }

            using (var scope = provider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
                var admin = await dbContext.BootstrapAdministrators.SingleAsync();
                Assert.DoesNotContain("Password123!", admin.PasswordHash);
            }
        }
    }

    private static (ServiceProvider Provider, string InMemoryToken) BuildServiceProvider()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);
        var inMemoryToken = $"{DynamicCommerceDbContextConfigurator.InMemoryConnectionToken}:{Guid.NewGuid():N}";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Commerce:ApplicationName"] = "Commerce"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(contentRoot));
        services.AddLogging();
        services.AddOptions<CommerceDataOptions>().Configure(options =>
        {
            options.Provider = CommerceDatabaseProvider.SqlServer;
            options.ConnectionString = string.Empty;
        });

        services.AddSingleton<IInstallationConnectionProvider, FileInstallationConnectionProvider>();
        services.AddSingleton<ICommerceDbContextConfigurator, DynamicCommerceDbContextConfigurator>();
        services.AddSingleton<InstallationRequirementsEvaluator>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<ICommerceMigration, CoreInitialMigration>();
        services.AddSingleton<MigrationRegistry>();
        services.AddSingleton<ICommerceSeeder, InstallationMetadataSeeder>();
        services.AddSingleton<ICommerceSeeder, DefaultSettingsSeeder>();

        services.AddScoped<MigrationRunner>();
        services.AddScoped<SeederRunner>();
        services.AddSingleton<ICommerceModuleManager, NoOpModuleManager>();
        services.AddSingleton<IStoreContextInitializerService, NoOpStoreContextInitializer>();
        services.AddScoped<IInstallationStateService, InstallationStateService>();
        services.AddScoped<IInstallationService, InstallationService>();

        services.AddCommerceDbContext();

        return (services.BuildServiceProvider(), inMemoryToken);
    }

    private sealed class NoOpModuleManager : ICommerceModuleManager
    {
        public IReadOnlyList<ModuleRuntimeInfo> DiscoverModules() => Array.Empty<ModuleRuntimeInfo>();

        public void ValidateModules()
        {
        }

        public IReadOnlyList<ModuleDescriptor> ResolveDependencies() => Array.Empty<ModuleDescriptor>();

        public void RegisterModules()
        {
        }

        public Task InitializeModulesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StartModulesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopModulesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpStoreContextInitializer : IStoreContextInitializerService
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Commerce.Tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
