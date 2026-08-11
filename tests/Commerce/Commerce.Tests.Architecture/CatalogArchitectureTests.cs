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

    [Theory]
    [InlineData(typeof(Commerce.Catalog.Domain.Entities.Product))]
    [InlineData(typeof(Commerce.Catalog.Contracts.Pricing.IPricingService))]
    public void CatalogLayers_DoNotReferenceFutureCommerceModules(Type anchorType)
    {
        var references = anchorType.Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        string[] forbidden =
        [
            "Commerce.Orders",
            "Commerce.Checkout",
            "Commerce.Payments",
            "Commerce.Shipping",
            "Commerce.Inventory.Infrastructure",
            "Commerce.Inventory.Application",
            "Commerce.ShoppingCart",
            "Commerce.Discounts"
        ];

        foreach (var forbiddenReference in forbidden)
        {
            Assert.DoesNotContain(references, x => x.Equals(forbiddenReference, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CatalogApplication_ReferencesInventoryContractsOnly()
    {
        var references = typeof(Commerce.Catalog.Application.Pricing.PricingService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Inventory.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Inventory.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Inventory.Application", StringComparison.Ordinal));
    }
}
