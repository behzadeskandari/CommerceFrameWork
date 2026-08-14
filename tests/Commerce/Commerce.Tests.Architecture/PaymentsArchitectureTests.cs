namespace Commerce.Tests.Architecture;

public sealed class PaymentsArchitectureTests
{
    [Fact]
    public void PaymentsDomain_DoesNotReferenceAspNetOrEfCore()
    {
        var references = typeof(Commerce.Payments.Domain.Entities.Payment).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void PaymentsApplication_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Payments.Application.Payments.PaymentService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Payments.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckoutApplication_DoesNotReferencePaymentsInfrastructure()
    {
        var references = typeof(Commerce.Checkout.Application.Checkout.CheckoutService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Payments.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void PaymentsContracts_DoesNotReferenceInfrastructure()
    {
        var references = typeof(Commerce.Payments.Contracts.Payments.IPaymentService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Payments.Infrastructure", StringComparison.Ordinal));
    }
}
