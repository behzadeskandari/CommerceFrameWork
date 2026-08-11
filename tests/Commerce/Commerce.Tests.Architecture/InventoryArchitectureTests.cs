using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class InventoryArchitectureTests
{
    [Fact]
    public void InventoryDomain_DoesNotReferenceAspNetOrEfCore()
    {
        var references = typeof(Commerce.Inventory.Domain.Entities.InventoryItem).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void InventoryApplication_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Inventory.Application.Inventory.InventoryReader).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Inventory.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.EndsWith(".Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void OrdersDomain_DoesNotReferenceInventoryInfrastructure()
    {
        var references = typeof(Commerce.Orders.Domain.Entities.Order).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Inventory.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CatalogDomain_DoesNotReferenceInventoryInfrastructure()
    {
        var references = typeof(Commerce.Catalog.Domain.Entities.Product).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Inventory.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void InventoryApplication_DoesNotReferencePaymentOrShippingInfrastructure()
    {
        var references = typeof(Commerce.Inventory.Application.Inventory.InventoryReader).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Contains("Payment", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, x => x.Contains("Shipping", StringComparison.OrdinalIgnoreCase));
    }
}
