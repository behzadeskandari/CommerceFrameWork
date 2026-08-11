using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class OrdersArchitectureTests
{
    [Fact]
    public void OrdersDomain_DoesNotReferenceAspNetOrEfCore()
    {
        var assembly = typeof(Commerce.Orders.Domain.Entities.Order).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void OrdersApplication_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Orders.Application.Orders.OrderService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Orders.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Checkout.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Cart.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Catalog.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Customers.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void OrdersApplication_UsesContractsOnly()
    {
        var references = typeof(Commerce.Orders.Application.Orders.OrderService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Checkout.Contracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Cart.Contracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Customers.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.EndsWith(".Infrastructure", StringComparison.Ordinal));
    }
}
