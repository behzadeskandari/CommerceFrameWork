using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Commerce.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class CartFlowTests
{
    [Fact]
    public async Task GuestCart_AddUpdateRemoveClear_WorkEndToEnd()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CART-GUEST-1", 19.99m);

        var add = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        using var addJson = JsonDocument.Parse(await add.Content.ReadAsStreamAsync());
        var cart = addJson.RootElement.GetProperty("data");
        Assert.Equal(2, cart.GetProperty("itemCount").GetInt32());
        var itemId = cart.GetProperty("items")[0].GetProperty("id").GetInt32();

        var update = await client.PutAsJsonAsync($"/api/cart/items/{itemId}", new { Quantity = 3 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var get = await client.GetAsync("/api/cart");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var getJson = JsonDocument.Parse(await get.Content.ReadAsStreamAsync());
        Assert.Equal(3, getJson.RootElement.GetProperty("data").GetProperty("itemCount").GetInt32());

        var remove = await client.DeleteAsync($"/api/cart/items/{itemId}");
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);

        var clear = await client.DeleteAsync("/api/cart");
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        using var clearJson = JsonDocument.Parse(await clear.Content.ReadAsStreamAsync());
        Assert.Equal(0, clearJson.RootElement.GetProperty("data").GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task CustomerCart_PersistsAcrossRequests()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, "cart-customer@example.com", "Password123!");
        var offerId = await CreateSimpleProductOfferAsync(client, "CART-CUST-1", 12.50m);

        var add = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        var get = await client.GetAsync("/api/cart");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var getJson = JsonDocument.Parse(await get.Content.ReadAsStreamAsync());
        Assert.Equal(1, getJson.RootElement.GetProperty("data").GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task GuestToCustomerMerge_CombinesDuplicateOffers()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CART-MERGE-1", 9.99m);

        var guestAdd = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, guestAdd.StatusCode);

        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, "cart-merge@example.com", "Password123!");

        var merge = await client.PostAsync("/api/cart/merge", null);
        Assert.Equal(HttpStatusCode.OK, merge.StatusCode);
        using var mergeJson = JsonDocument.Parse(await merge.Content.ReadAsStreamAsync());
        var cart = mergeJson.RootElement.GetProperty("data").GetProperty("cart");
        Assert.Equal(2, cart.GetProperty("itemCount").GetInt32());

        var customerAdd = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, customerAdd.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerAdd.Content.ReadAsStreamAsync());
        Assert.Equal(3, customerJson.RootElement.GetProperty("data").GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task AddItem_RejectsCrossStoreOffer()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var languages = await client.GetAsync("/api/languages");
        using var languagesJson = JsonDocument.Parse(await languages.Content.ReadAsStreamAsync());
        var languageId = languagesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var currencies = await client.GetAsync("/api/currencies");
        using var currenciesJson = JsonDocument.Parse(await currencies.Content.ReadAsStreamAsync());
        var currencyId = currenciesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var storeResponse = await client.PostAsJsonAsync("/api/stores", new
        {
            SystemName = "cart-store-b",
            Name = "Cart Store B",
            Url = "https://cart-store-b.test",
            DefaultLanguageId = languageId,
            DefaultCurrencyId = currencyId,
            Domains = new[]
            {
                new { Host = "cart-store-b.test", Scheme = "https", Port = (int?)443, IsPrimary = true, IsSslRequired = true }
            }
        });
        Assert.Equal(HttpStatusCode.Created, storeResponse.StatusCode);
        using var storeJson = JsonDocument.Parse(await storeResponse.Content.ReadAsStreamAsync());
        var otherStoreId = storeJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var offerId = await CreateSimpleProductOfferAsync(client, "CART-STORE-ISO", 5m, storeIdOverride: otherStoreId, host: "cart-store-b.test");

        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = "localhost";
        await client.SendAsync(contextRequest);

        var add = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, add.StatusCode);
    }

    [Fact]
    public async Task AddItem_RejectsInactiveOffer()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CART-INACTIVE", 15m, isActive: false);

        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = "localhost";
        await client.SendAsync(contextRequest);

        var add = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, add.StatusCode);
    }

    [Fact]
    public async Task DuplicateAddItem_AccumulatesQuantity()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CART-DUP-1", 4m);

        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 2 });
        var second = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 3 });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var json = JsonDocument.Parse(await second.Content.ReadAsStreamAsync());
        Assert.Equal(5, json.RootElement.GetProperty("data").GetProperty("itemCount").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("data").GetProperty("items").GetArrayLength());
    }

    private static HttpClient CreateStorefrontClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task<int> CreateSimpleProductOfferAsync(
        HttpClient client,
        string sku,
        decimal price,
        int? storeIdOverride = null,
        bool isActive = true,
        string host = "localhost")
    {
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var productResponse = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            Name = sku,
            Sku = sku,
            ProductType = ProductType.Simple,
            Published = true,
            IsVisible = true,
            IsAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var currencies = await client.GetAsync("/api/currencies");
        using var currenciesJson = JsonDocument.Parse(await currencies.Content.ReadAsStreamAsync());
        var stores = await client.GetAsync("/api/stores");
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        var storeId = storeIdOverride ?? storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = host;
        var contextResponse = await client.SendAsync(contextRequest);
        using var contextJson = JsonDocument.Parse(await contextResponse.Content.ReadAsStreamAsync());
        var contextCurrencyId = contextJson.RootElement.GetProperty("data").GetProperty("currencyId").GetInt32();
        var currencyCode = currenciesJson.RootElement.GetProperty("data")
            .EnumerateArray()
            .First(x => x.GetProperty("id").GetInt32() == contextCurrencyId)
            .GetProperty("code")
            .GetString();

        var offerResponse = await client.PostAsJsonAsync("/api/catalog/offers", new
        {
            ProductId = productId,
            StoreId = storeId,
            CurrencyId = contextCurrencyId,
            CurrencyCode = currencyCode,
            Price = price,
            IsActive = isActive
        });
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        using var offerJson = JsonDocument.Parse(await offerResponse.Content.ReadAsStreamAsync());
        var offerId = offerJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var logout = await client.PostAsync("/api/customers/logout", null);
        if (logout.StatusCode == HttpStatusCode.Unauthorized)
        {
            await client.PostAsync("/api/auth/logout", null);
        }

        return offerId;
    }
}
