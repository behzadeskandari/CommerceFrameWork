using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class PricingArchitectureTests
{
    [Fact]
    public void PricingDomain_DoesNotReferenceAspNetOrEfCore()
    {
        var references = typeof(Commerce.Pricing.Domain.Entities.Discount).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void PricingApplication_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Pricing.Application.Pricing.PriceCalculationService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Pricing.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void OrdersDomain_DoesNotReferencePricingInfrastructure()
    {
        var references = typeof(Commerce.Orders.Domain.Entities.Order).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Pricing.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CartDomain_DoesNotReferencePricingInfrastructure()
    {
        var references = typeof(Commerce.Cart.Domain.Entities.ShoppingCart).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Pricing.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckoutDomain_DoesNotReferencePricingInfrastructure()
    {
        var references = typeof(Commerce.Checkout.Domain.Entities.CheckoutSession).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Pricing.Infrastructure", StringComparison.Ordinal));
    }
}
