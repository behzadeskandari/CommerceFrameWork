using System.Reflection;
using Xunit;

namespace Commerce.Tests.Architecture;

public sealed class DownloadsArchitectureTests
{
    [Fact]
    public void DownloadsApplication_DoesNotReferenceMediaImplementation()
    {
        var assembly = typeof(Commerce.Downloads.Application.Storefront.CustomerDownloadService).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Media.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Media.Application", StringComparison.Ordinal));
    }

    [Fact]
    public void DownloadsDomain_DoesNotReferenceInfrastructure()
    {
        var assembly = typeof(Commerce.Downloads.Domain.Entities.DownloadEntitlement).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(references, x => x.Contains("Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DownloadsApplication_DoesNotReferenceHost()
    {
        var references = typeof(Commerce.Downloads.Application.Storefront.CustomerDownloadService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
    }
}
