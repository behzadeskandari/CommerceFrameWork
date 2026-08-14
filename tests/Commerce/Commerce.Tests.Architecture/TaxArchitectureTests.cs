using Xunit;



namespace Commerce.Tests.Architecture;



public sealed class TaxArchitectureTests

{

    [Fact]

    public void TaxDomain_DoesNotReferenceAspNetOrEfCore()

    {

        var references = typeof(Commerce.Tax.Domain.Entities.TaxCategory).Assembly

            .GetReferencedAssemblies()

            .Select(a => a.Name ?? string.Empty)

            .ToList();



        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));

    }



    [Fact]

    public void TaxApplication_DoesNotReferenceInfrastructure()

    {

        var references = typeof(Commerce.Tax.Application.Tax.TaxCalculationService).Assembly

            .GetReferencedAssemblies()

            .Select(a => a.Name ?? string.Empty)

            .ToList();



        Assert.DoesNotContain(references, x => x.Equals("Commerce.Tax.Infrastructure", StringComparison.Ordinal));

    }



    [Fact]

    public void CheckoutApplication_DoesNotReferenceTaxInfrastructure()

    {

        var references = typeof(Commerce.Checkout.Application.Checkout.CheckoutService).Assembly

            .GetReferencedAssemblies()

            .Select(a => a.Name ?? string.Empty)

            .ToList();



        Assert.DoesNotContain(references, x => x.Equals("Commerce.Tax.Infrastructure", StringComparison.Ordinal));

    }



    [Fact]

    public void TaxContracts_DoesNotReferenceInfrastructure()

    {

        var references = typeof(Commerce.Tax.Contracts.Tax.ITaxCalculationService).Assembly

            .GetReferencedAssemblies()

            .Select(a => a.Name ?? string.Empty)

            .ToList();



        Assert.DoesNotContain(references, x => x.Equals("Commerce.Tax.Infrastructure", StringComparison.Ordinal));

    }

}


