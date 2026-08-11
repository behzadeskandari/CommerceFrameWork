using System.Reflection;
using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class MediaArchitectureTests
{
    [Fact]
    public void CatalogApplication_ReferencesMediaContractsOnly()
    {
        var assembly = typeof(Commerce.Catalog.Application.Products.ProductService).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Media.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Media.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void MediaDomain_DoesNotReferenceSystemIo()
    {
        var assembly = typeof(Commerce.Media.Domain.Entities.MediaAsset).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name == "System.IO");
    }

    [Fact]
    public void CatalogApplication_DoesNotReferenceLocalMediaStorage()
    {
        var references = typeof(Commerce.Catalog.Application.Products.ProductService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(references, x => x.Contains("LocalMediaStorage", StringComparison.OrdinalIgnoreCase));
    }
}
