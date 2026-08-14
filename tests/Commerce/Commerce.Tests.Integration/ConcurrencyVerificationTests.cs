using System.Net;
using System.Net.Http.Json;
using Commerce.Catalog.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Integration;

[Trait("Category", "Concurrency")]
[Trait("Phase", "45")]
public sealed class ConcurrencyVerificationTests
{
    [Fact]
    public async Task OrderCreation_SameIdempotencyKey_ReturnsSameOrder()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationWorkflowHelper.EnsureManualPaymentPluginAsync(client);

        var offerId = await IntegrationWorkflowHelper.CreateProductOfferAsync(
            client, "CONC-IDEM-1", ProductType.Simple, 15m);

        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        var start = await client.PostAsync("/api/checkout", null);
        start.EnsureSuccessStatusCode();

        using var startJson = System.Text.Json.JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "guest@example.com" });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new
        {
            Address = new
            {
                FirstName = "Guest",
                LastName = "User",
                Country = "IR",
                City = "Tehran",
                Address1 = "Street 1",
                PostalCode = "1234567890"
            }
        });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-address", new
        {
            Address = new
            {
                FirstName = "Guest",
                LastName = "User",
                Country = "IR",
                City = "Tehran",
                Address1 = "Street 1",
                PostalCode = "1234567890"
            }
        });
        await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);

        var key = Guid.NewGuid().ToString("N");
        var first = await SendCreateOrderAsync(client, checkoutId, key);
        var second = await SendCreateOrderAsync(client, checkoutId, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        using var firstJson = System.Text.Json.JsonDocument.Parse(await first.Content.ReadAsStreamAsync());
        using var secondJson = System.Text.Json.JsonDocument.Parse(await second.Content.ReadAsStreamAsync());
        Assert.Equal(
            firstJson.RootElement.GetProperty("data").GetProperty("id").GetInt32(),
            secondJson.RootElement.GetProperty("data").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task ParallelHealthChecks_MostSucceed()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => client.GetAsync("/health/live"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        var success = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.True(success >= 28, $"Expected at least 28/32 successful health checks, got {success}");
    }

    private static Task<HttpResponseMessage> SendCreateOrderAsync(HttpClient client, int checkoutId, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new { CheckoutId = checkoutId })
        };
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
}
