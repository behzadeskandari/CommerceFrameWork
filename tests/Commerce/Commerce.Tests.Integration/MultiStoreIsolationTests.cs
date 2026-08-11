using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class MultiStoreIsolationTests
{
    [Fact]
    public async Task HostHeader_ResolvesCorrectStore_WithoutCrossStoreLeakage()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var languagesResponse = await client.GetAsync("/api/languages");
        Assert.Equal(HttpStatusCode.OK, languagesResponse.StatusCode);
        using var languagesJson = JsonDocument.Parse(await languagesResponse.Content.ReadAsStreamAsync());
        var languageId = languagesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var currenciesResponse = await client.GetAsync("/api/currencies");
        Assert.Equal(HttpStatusCode.OK, currenciesResponse.StatusCode);
        using var currenciesJson = JsonDocument.Parse(await currenciesResponse.Content.ReadAsStreamAsync());
        var currencyId = currenciesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var storeAResponse = await client.PostAsJsonAsync("/api/stores", new
        {
            SystemName = "store-a",
            Name = "Store A",
            Url = "https://store-a.test",
            DefaultLanguageId = languageId,
            DefaultCurrencyId = currencyId,
            Domains = new[]
            {
                new { Host = "store-a.test", Scheme = "https", Port = (int?)443, IsPrimary = true, IsSslRequired = true }
            }
        });
        Assert.Equal(HttpStatusCode.Created, storeAResponse.StatusCode);
        using var storeAJson = JsonDocument.Parse(await storeAResponse.Content.ReadAsStreamAsync());
        var storeAId = storeAJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var storeBResponse = await client.PostAsJsonAsync("/api/stores", new
        {
            SystemName = "store-b",
            Name = "Store B",
            Url = "https://store-b.test",
            DefaultLanguageId = languageId,
            DefaultCurrencyId = currencyId,
            Domains = new[]
            {
                new { Host = "store-b.test", Scheme = "https", Port = (int?)443, IsPrimary = true, IsSslRequired = true }
            }
        });
        Assert.Equal(HttpStatusCode.Created, storeBResponse.StatusCode);
        using var storeBJson = JsonDocument.Parse(await storeBResponse.Content.ReadAsStreamAsync());
        var storeBId = storeBJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        using var contextA = await GetStoreContextAsync(client, "store-a.test");
        var dataA = contextA.RootElement.GetProperty("data");
        Assert.Equal(storeAId, dataA.GetProperty("storeId").GetInt32());
        Assert.Equal("store-a", dataA.GetProperty("storeSystemName").GetString());

        using var contextB = await GetStoreContextAsync(client, "store-b.test");
        var dataB = contextB.RootElement.GetProperty("data");
        Assert.Equal(storeBId, dataB.GetProperty("storeId").GetInt32());
        Assert.Equal("store-b", dataB.GetProperty("storeSystemName").GetString());

        Assert.NotEqual(
            dataA.GetProperty("storeId").GetInt32(),
            dataB.GetProperty("storeId").GetInt32());
    }

    private static async Task<JsonDocument> GetStoreContextAsync(HttpClient client, string host)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        request.Headers.Host = host;
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
    }
}
