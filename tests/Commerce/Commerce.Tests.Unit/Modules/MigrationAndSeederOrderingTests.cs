using Commerce.Framework.Application.Modules;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Data.Seeding;
using Commerce.Modules.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commerce.Tests.Unit.Modules;

public sealed class MigrationAndSeederOrderingTests
{
    [Fact]
    public void MigrationRegistry_OrdersByModuleDependency()
    {
        var moduleContext = CreateContext([
            new AlphaTestModule(),
            new ModuleMigrationTestModule()
        ]);

        var registry = new MigrationRegistry([
            new TestModuleMigration(),
            new Commerce.Framework.Data.Migrations.Core.CoreInitialMigration()
        ], moduleContext.OrderedSystemNames);

        var ordered = registry.GetOrdered().Select(x => x.Module).ToArray();

        Assert.Equal("Core", ordered[0]);
        Assert.Equal("Commerce.Test.Migration", ordered[1]);
    }

    [Fact]
    public void SeederRunner_OrdersByModuleDependency()
    {
        var moduleContext = CreateContext([
            new AlphaTestModule(),
            new ModuleSeederTestModule()
        ]);

        var runner = new SeederRunner(
            [
                new TestModuleSeeder(),
                new DefaultSettingsSeeder()
            ],
            new ServiceCollection().BuildServiceProvider(),
            moduleContext,
            NullLogger<SeederRunner>.Instance);

        var ordered = GetPrivateSeeders(runner).Select(GetSeederName).ToArray();

        Assert.Equal("DefaultSettings", ordered[0]);
        Assert.Equal("TestModuleSeeder", ordered[1]);
    }

    private static ModuleRegistrationContext CreateContext(IReadOnlyList<Commerce.Framework.Contracts.Modules.ICommerceModule> modules)
    {
        var descriptors = modules.Select(x => x.Descriptor).ToList();
        var ordered = ModuleDependencyResolver.Resolve(descriptors);
        var orderedModules = ordered
            .Select(descriptor => modules.Single(m => m.Descriptor.SystemName == descriptor.SystemName))
            .ToList();

        return new ModuleRegistrationContext(orderedModules, ordered, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ICommerceSeeder> GetPrivateSeeders(SeederRunner runner)
    {
        var field = typeof(SeederRunner).GetField("_seeders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (IReadOnlyList<ICommerceSeeder>)field!.GetValue(runner)!;
    }

    private static string GetSeederName(ICommerceSeeder seeder) => seeder.Name;
}
