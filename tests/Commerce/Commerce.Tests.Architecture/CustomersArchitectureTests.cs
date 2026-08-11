using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class CustomersArchitectureTests
{
    [Fact]
    public void CustomersDomain_DoesNotReferenceInfrastructureOrIdentity()
    {
        var assembly = typeof(Commerce.Customers.Domain.Entities.Customer).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, x => x.Contains("EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Customers.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CustomersContracts_DoesNotReferenceInfrastructure()
    {
        var assembly = typeof(Commerce.Customers.Contracts.Customers.ICustomerReader).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Customers.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_DoesNotReferenceCustomersInfrastructure()
    {
        var catalogAssembly = typeof(Commerce.Catalog.Domain.Entities.Product).Assembly;
        var references = catalogAssembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Customers.Infrastructure", StringComparison.Ordinal));
    }
}
