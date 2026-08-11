using System.Reflection;
using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class CartArchitectureTests
{
    [Fact]
    public void CartDomain_DoesNotReferenceAspNetOrEfCore()
    {
        var assembly = typeof(Commerce.Cart.Domain.Entities.ShoppingCart).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void CartApplication_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Cart.Application.Carts.CartService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Cart.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Catalog.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Media.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CartApplication_ReferencesCatalogContractsOnly()
    {
        var references = typeof(Commerce.Cart.Application.Carts.CartService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Catalog.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Catalog.Infrastructure", StringComparison.Ordinal));
    }
}
