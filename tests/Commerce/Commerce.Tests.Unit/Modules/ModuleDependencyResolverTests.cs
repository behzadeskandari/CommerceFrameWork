using Commerce.Framework.Application.Modules;
using Commerce.Framework.Contracts.Modules;
using Commerce.Modules.TestSupport;
using Xunit;

namespace Commerce.Tests.Unit.Modules;

public sealed class ModuleDependencyResolverTests
{
    [Fact]
    public void Resolve_WithNoDependencies_ReturnsSingleModule()
    {
        var module = new AlphaTestModule().Descriptor;
        var ordered = ModuleDependencyResolver.Resolve([module]);

        Assert.Single(ordered);
        Assert.Equal("Commerce.Test.Alpha", ordered[0].SystemName);
    }

    [Fact]
    public void Resolve_WithDependencyChain_OrdersDependenciesFirst()
    {
        var ordered = ModuleDependencyResolver.Resolve([
            new GammaTestModule().Descriptor,
            new BetaTestModule().Descriptor,
            new AlphaTestModule().Descriptor
        ]);

        Assert.Equal(
            ["Commerce.Test.Alpha", "Commerce.Test.Beta", "Commerce.Test.Gamma"],
            ordered.Select(x => x.SystemName).ToArray());
    }

    [Fact]
    public void Resolve_WithMissingDependency_ThrowsActionableError()
    {
        var exception = Assert.Throws<ModuleDependencyResolutionException>(() =>
            ModuleDependencyResolver.Resolve([new MissingDependencyModule().Descriptor]));

        Assert.Contains("Commerce.Test.NotInstalled", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WithCircularDependency_ThrowsActionableError()
    {
        var exception = Assert.Throws<ModuleDependencyResolutionException>(() =>
            ModuleDependencyResolver.Resolve([
                new CircularModuleA().Descriptor,
                new CircularModuleB().Descriptor
            ]));

        Assert.Contains("Circular", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_WithDuplicateSystemName_ThrowsActionableError()
    {
        var first = new AlphaTestModule().Descriptor;
        var duplicate = first with { Id = "test.alpha.duplicate" };

        var exception = Assert.Throws<ModuleDependencyResolutionException>(() =>
            ModuleDependencyResolver.Resolve([first, duplicate]));

        Assert.Contains("Duplicate module SystemName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WithIncompatibleVersion_ThrowsActionableError()
    {
        var module = new ModuleDescriptor(
            "test.incompatible",
            "Commerce.Test.Incompatible",
            "Incompatible",
            new Version(1, 0, 0),
            "Requires newer alpha.",
            [new ModuleDependency("Commerce.Test.Alpha", "2.0.0")]);

        var exception = Assert.Throws<ModuleDependencyResolutionException>(() =>
            ModuleDependencyResolver.Resolve([module, new AlphaTestModule().Descriptor]));

        Assert.Contains("requires 'Commerce.Test.Alpha' version 2.0.0", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WithDisabledModule_ExcludesDisabledModule()
    {
        var ordered = ModuleDependencyResolver.Resolve(
            [new AlphaTestModule().Descriptor, new BetaTestModule().Descriptor],
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Commerce.Test.Beta" });

        Assert.Single(ordered);
        Assert.Equal("Commerce.Test.Alpha", ordered[0].SystemName);
    }
}
