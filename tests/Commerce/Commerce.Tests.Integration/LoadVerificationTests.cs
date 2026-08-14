using System.Diagnostics;
using System.Net;
using Commerce.Catalog.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Integration;

[Trait("Category", "Load")]
[Trait("Phase", "45")]
public sealed class LoadVerificationTests
{
    [Fact]
    public async Task StorefrontCatalog_50ParallelRequests_MeetsLatencyBudget()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        await IntegrationWorkflowHelper.CreateProductOfferAsync(client, "LOAD-1", ProductType.Simple, 9.99m);

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => client.GetAsync("/api/catalog/storefront/products?term=LOAD"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        var ok = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.True(ok >= 45, $"Expected >=45/50 OK responses, got {ok}");
        Assert.True(sw.ElapsedMilliseconds < 30_000, $"Load test exceeded 30s budget: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task HealthReady_20SequentialRequests_AllHealthyOrDegraded()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        for (var i = 0; i < 20; i++)
        {
            var response = await client.GetAsync("/health/ready");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"health/ready returned {response.StatusCode} on iteration {i}");
        }
    }
}
