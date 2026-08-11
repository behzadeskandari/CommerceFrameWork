using System.Reflection;
using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class CheckoutArchitectureTests
{
    [Fact]
    public void CheckoutDomain_DoesNotReferenceAspNetOrEfCore()
    {
        var assembly = typeof(Commerce.Checkout.Domain.Entities.CheckoutSession).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckoutApplication_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Checkout.Application.Checkout.CheckoutService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Checkout.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Catalog.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Cart.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Customers.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckoutApplication_UsesContractsOnly()
    {
        var references = typeof(Commerce.Checkout.Application.Checkout.CheckoutService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Cart.Contracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Catalog.Contracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Customers.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.EndsWith(".Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckoutInfrastructure_DoesNotReferenceOrderOrPaymentInfrastructure()
    {
        var references = typeof(Commerce.Checkout.Infrastructure.Persistence.CheckoutModelContributor).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Contains("Order", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, x => x.Contains("Payment", StringComparison.OrdinalIgnoreCase));
    }
}
