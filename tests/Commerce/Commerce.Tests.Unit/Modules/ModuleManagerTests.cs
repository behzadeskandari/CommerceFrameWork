using Commerce.Framework.Application.DependencyInjection;
using Commerce.Framework.Application.Modules;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Data.Tenancy;
using Commerce.Modules.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Commerce.Tests.Unit.Modules;

public sealed class ModuleManagerTests
{
    [Fact]
    public async Task StartModulesAsync_RunsLifecycleInDependencyOrder()
    {
        var services = BuildServices(modules => modules.AddModule<AlphaTestModule>().AddModule<BetaTestModule>().AddModule<GammaTestModule>());
        using var provider = services.BuildServiceProvider();

        var manager = provider.GetRequiredService<ICommerceModuleManager>();
        manager.RegisterModules();
        await manager.InitializeModulesAsync();
        await manager.StartModulesAsync();

        var context = provider.GetRequiredService<ModuleRegistrationContext>();
        var alpha = (AlphaTestModule)context.Modules.Single(m => m.Descriptor.SystemName == "Commerce.Test.Alpha");

        var registry = provider.GetRequiredService<ICommerceModuleRegistry>();
        var modules = registry.GetModulesInDependencyOrder();

        Assert.All(modules, module => Assert.Equal(ModuleState.Started, module.State));
        Assert.True(alpha.Started);
    }

    [Fact]
    public async Task StartModulesAsync_WhenRequiredModuleFails_Throws()
    {
        var services = BuildServices(modules => modules.AddModule<FailingTestModule>());
        using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<ICommerceModuleManager>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.InitializeModulesAsync());
    }

    [Fact]
    public void AddCommerceModules_RegistersModuleOwnedMigration()
    {
        var services = BuildServices(modules =>
            modules.AddModule<AlphaTestModule>().AddModule<ModuleMigrationTestModule>());

        using var provider = services.BuildServiceProvider();
        var migrations = provider.GetServices<Commerce.Framework.Data.Migrations.ICommerceMigration>().ToList();

        Assert.Contains(migrations, x => x.Module == "Commerce.Test.Migration");
    }

    private static ServiceCollection BuildServices(Action<ModuleRegistrationBuilder> configureModules)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Commerce:ApplicationName"] = "Commerce"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IInstallationStateService, InstalledInstallationStateService>();
        services.AddScoped<StoreContext>();
        services.AddScoped<IStoreContext>(sp => sp.GetRequiredService<StoreContext>());
        services.AddCommerceModules(configuration, configureModules);
        return services;
    }

    private sealed class InstalledInstallationStateService : IInstallationStateService
    {
        public Task<InstallationStateInfo> GetStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new InstallationStateInfo(
                InstallationStatus.Installed,
                InstallationStep.Complete,
                true,
                "1.0.0",
                DateTime.UtcNow,
                null));

        public Task<bool> IsInstalledAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> IsInstallationLockedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
