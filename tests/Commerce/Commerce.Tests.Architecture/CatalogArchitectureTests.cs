using System.Reflection;
using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class CatalogArchitectureTests
{
    [Fact]
    public void CatalogDomain_DoesNotReferenceInfrastructureOrEfCore()
    {
        var assembly = typeof(Commerce.Catalog.Domain.Entities.Product).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, x => x.Contains("EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Catalog.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("AspNetCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CatalogContracts_DoesNotReferenceInfrastructure()
    {
        var assembly = typeof(Commerce.Catalog.Contracts.Products.IProductReader).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Catalog.Infrastructure", StringComparison.Ordinal));
    }
}
