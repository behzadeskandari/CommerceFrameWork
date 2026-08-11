using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class StoreArchitectureTests
{
    [Fact]
    public void StoreDomain_DoesNotReferenceInfrastructureOrHost()
    {
        var assembly = typeof(Commerce.Store.Domain.Entities.Store).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, x => x.Contains("EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Store.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StoreContracts_DoesNotReferenceInfrastructure()
    {
        var assembly = typeof(Commerce.Store.Contracts.Stores.IStoreReader).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Store.Infrastructure", StringComparison.Ordinal));
    }
}
