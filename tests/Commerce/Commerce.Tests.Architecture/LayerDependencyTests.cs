using System.Reflection;
using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Core_DoesNotReferenceHigherLayers()
    {
        var references = GetReferences(typeof(Commerce.Framework.Core.Results.Result).Assembly);

        AssertDoesNotContainAny(references,
            "Commerce.Framework.Domain",
            "Commerce.Framework.Contracts",
            "Commerce.Framework.Application",
            "Commerce.Framework.Infrastructure",
            "Commerce.Framework.Data");
    }

    [Fact]
    public void Domain_DoesNotReferenceInfrastructureOrData()
    {
        var references = GetReferences(typeof(Commerce.Framework.Domain.ValueObjects.Money).Assembly);

        AssertDoesNotContainAny(references,
            "Commerce.Framework.Infrastructure",
            "Commerce.Framework.Data",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Contracts_DoesNotReferenceInfrastructureOrData()
    {
        var references = GetReferences(typeof(Commerce.Framework.Contracts.Modules.ICommerceModule).Assembly);

        AssertDoesNotContainAny(references,
            "Commerce.Framework.Infrastructure",
            "Commerce.Framework.Data",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrData()
    {
        var references = GetReferences(typeof(Commerce.Framework.Application.Validation.ValidationResult).Assembly);

        AssertDoesNotContainAny(references,
            "Commerce.Framework.Infrastructure",
            "Commerce.Framework.Data",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void FrameworkProjects_DoNotReferenceHost()
    {
        var frameworkAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
            {
                var name = a.GetName().Name;
                return name is not null &&
                       name.StartsWith("Commerce.Framework.", StringComparison.Ordinal);
            })
            .ToList();

        foreach (var assembly in frameworkAssemblies)
        {
            var references = GetReferences(assembly);
            Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CommerceProjects_DoNotReferenceBankingAssemblies()
    {
        var commerceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Commerce.", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var assembly in commerceAssemblies)
        {
            var references = GetReferences(assembly);
            Assert.All(references, reference =>
            {
                Assert.DoesNotMatch("^(Gateway|Bank1|Bank2)\\.", reference);
            });
        }
    }

    [Fact]
    public void CommerceProjects_TargetNet10()
    {
        var commerceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Commerce.", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var assembly in commerceAssemblies)
        {
            var framework = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName;
            Assert.NotNull(framework);
            Assert.Contains("10.0", framework, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<string> GetReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

    private static void AssertDoesNotContainAny(IReadOnlyList<string> references, params string[] forbidden)
    {
        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(references, reference =>
                reference.Contains(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
