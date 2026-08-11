using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Data.Migrations.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commerce.Tests.Unit.Data;

public sealed class MigrationRegistryTests
{
    [Fact]
    public void GetOrdered_OrdersByModuleAndVersion()
    {
        var registry = new MigrationRegistry([
            new TestMigration("Catalog", "2.0.0", "Catalog_Second"),
            new TestMigration("Core", "1.0.0", "Core_First"),
            new TestMigration("Core", "1.1.0", "Core_Second")
        ]);

        var ordered = registry.GetOrdered();

        Assert.Equal("Core_First", ordered[0].Name);
        Assert.Equal("Core_Second", ordered[1].Name);
        Assert.Equal("Catalog_Second", ordered[2].Name);
    }

    [Fact]
    public void Constructor_WithDuplicateVersion_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new MigrationRegistry([
            new TestMigration("Core", "1.0.0", "Core_First"),
            new TestMigration("Core", "1.0.0", "Core_Duplicate")
        ]));
    }

    [Fact]
    public void Constructor_WithDuplicateName_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new MigrationRegistry([
            new TestMigration("Core", "1.0.0", "Same_Name"),
            new TestMigration("Catalog", "1.0.0", "Same_Name")
        ]));
    }

    private sealed class TestMigration(string module, string version, string name) : ICommerceMigration
    {
        public string Version { get; } = version;
        public string Name { get; } = name;
        public string Description => "Test migration";
        public string Module { get; } = module;

        public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

public sealed class MigrationRunnerTests
{
    [Fact]
    public async Task GetPendingMigrations_ReturnsUnappliedMigrations()
    {
        await using var context = CreateContext();
        var services = BuildServiceProvider(context);
        var runner = CreateRunner(context, services);

        var pending = await runner.GetPendingMigrationsAsync();

        Assert.Single(pending);
        Assert.Equal("Core_Initial", pending[0].Name);
    }

    [Fact]
    public async Task RunPendingMigrations_IsIdempotent()
    {
        await using var context = CreateContext();
        var services = BuildServiceProvider(context);
        var runner = CreateRunner(context, services);

        var firstRun = await runner.RunPendingMigrationsAsync();
        var secondRun = await runner.RunPendingMigrationsAsync();

        Assert.True(firstRun.IsSuccess);
        Assert.Equal(1, firstRun.Value);
        Assert.True(secondRun.IsSuccess);
        Assert.Equal(0, secondRun.Value);
        Assert.Equal(1, await context.MigrationVersionInfo.CountAsync());
    }

    private static CommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CommerceDbContext(options, new ServiceCollection().BuildServiceProvider());
    }

    private static ServiceProvider BuildServiceProvider(CommerceDbContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<ICommerceMigration, CoreInitialMigration>();
        services.AddSingleton<MigrationRegistry>();
        return services.BuildServiceProvider();
    }

    private static MigrationRunner CreateRunner(CommerceDbContext context, ServiceProvider services)
    {
        var registry = services.GetRequiredService<MigrationRegistry>();
        return new MigrationRunner(
            context,
            registry,
            services,
            NullLogger<MigrationRunner>.Instance);
    }
}
