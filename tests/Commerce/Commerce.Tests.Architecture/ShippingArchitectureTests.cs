using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class ShippingArchitectureTests
{
    [Fact]
    public void ShippingDomain_DoesNotReferenceAspNetOrEfCore()
    {
        var references = typeof(Commerce.Shipping.Domain.Entities.ShippingMethod).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void ShippingApplication_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Shipping.Application.Shipping.ShippingCalculationService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Shipping.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckoutApplication_DoesNotReferenceShippingInfrastructure()
    {
        var references = typeof(Commerce.Checkout.Application.Checkout.CheckoutService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Shipping.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void ShippingContracts_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Shipping.Contracts.Shipping.IShippingCalculationService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Shipping.Infrastructure", StringComparison.Ordinal));
    }
}
