using System.Reflection;
using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class ModuleDependencyTests
{
    [Fact]
    public void FrameworkProjects_DoNotReferenceCommerceModules()
    {
        var frameworkAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Commerce.Framework.", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var assembly in frameworkAssemblies)
        {
            var references = assembly.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            Assert.All(references, reference =>
            {
                Assert.DoesNotMatch("^Commerce\\.Modules\\.", reference);
            });
        }
    }

    [Fact]
    public void ModuleProjects_DoNotReferenceHost()
    {
        var moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Commerce.Modules.", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var assembly in moduleAssemblies)
        {
            var references = assembly.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ModuleProjects_DoNotReferenceBankingAssemblies()
    {
        var moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Commerce.Modules.", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var assembly in moduleAssemblies)
        {
            var references = assembly.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            Assert.All(references, reference =>
            {
                Assert.DoesNotMatch("^(Gateway|Bank1|Bank2)\\.", reference);
            });
        }
    }
}
