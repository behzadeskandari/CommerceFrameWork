using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.TestSupport;

public sealed class AlphaTestModule : CommerceModuleBase
{
    public bool Initialized { get; private set; }

    public bool Started { get; private set; }

    public override ModuleDescriptor Descriptor { get; } = new(
        "test.alpha",
        "Commerce.Test.Alpha",
        "Alpha Test Module",
        new Version(1, 0, 0),
        "Alpha test module.",
        Array.Empty<ModuleDependency>());

    public override Task InitializeAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default)
    {
        Initialized = true;
        return Task.CompletedTask;
    }

    public override Task StartAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default)
    {
        Started = true;
        return Task.CompletedTask;
    }
}

public sealed class BetaTestModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.beta",
        "Commerce.Test.Beta",
        "Beta Test Module",
        new Version(1, 0, 0),
        "Beta test module.",
        [new ModuleDependency("Commerce.Test.Alpha", "1.0.0")]);
}

public sealed class GammaTestModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.gamma",
        "Commerce.Test.Gamma",
        "Gamma Test Module",
        new Version(1, 0, 0),
        "Gamma test module.",
        [new ModuleDependency("Commerce.Test.Beta")]);
}

public sealed class MissingDependencyModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.missing",
        "Commerce.Test.Missing",
        "Missing Dependency Module",
        new Version(1, 0, 0),
        "Depends on a module that does not exist.",
        [new ModuleDependency("Commerce.Test.NotInstalled")]);
}

public sealed class CircularModuleA : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.circular.a",
        "Commerce.Test.CircularA",
        "Circular A",
        new Version(1, 0, 0),
        "Circular dependency A.",
        [new ModuleDependency("Commerce.Test.CircularB")]);
}

public sealed class CircularModuleB : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.circular.b",
        "Commerce.Test.CircularB",
        "Circular B",
        new Version(1, 0, 0),
        "Circular dependency B.",
        [new ModuleDependency("Commerce.Test.CircularA")]);
}

public sealed class FailingTestModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.failing",
        "Commerce.Test.Failing",
        "Failing Test Module",
        new Version(1, 0, 0),
        "Fails during initialization.",
        Array.Empty<ModuleDependency>(),
        IsRequired: true);

    public override Task InitializeAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Simulated module initialization failure.");
}

public sealed class ModuleMigrationTestModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.migration",
        "Commerce.Test.Migration",
        "Migration Test Module",
        new Version(1, 0, 0),
        "Registers a module-owned migration.",
        [new ModuleDependency("Commerce.Test.Alpha")]);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceMigration, TestModuleMigration>();
    }
}

public sealed class TestModuleMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "TestModule_Initial";

    public string Description => "Test module migration.";

    public string Module => "Commerce.Test.Migration";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class ModuleSeederTestModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "test.seeder",
        "Commerce.Test.Seeder",
        "Seeder Test Module",
        new Version(1, 0, 0),
        "Registers a module-owned seeder.",
        [new ModuleDependency("Commerce.Test.Alpha")]);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceSeeder, TestModuleSeeder>();
    }
}

public sealed class TestModuleSeeder : IModuleSeeder
{
    public int Order => 10;

    public string Name => "TestModuleSeeder";

    public string ModuleSystemName => "Commerce.Test.Seeder";

    public Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
