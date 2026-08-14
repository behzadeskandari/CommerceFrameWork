using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Commerce.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class CheckoutFlowTests
{
    [Fact]
    public async Task StartCheckout_GuestWithItems_Succeeds()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CHK-GUEST-1", 25m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        var startBody = await start.Content.ReadAsStringAsync();
        Assert.True(start.StatusCode == HttpStatusCode.OK, $"Status: {start.StatusCode}, Body: {startBody}");
        using var json = JsonDocument.Parse(startBody);
        var checkout = json.RootElement.GetProperty("data");
        Assert.True(checkout.GetProperty("id").GetInt32() > 0);
        Assert.Equal("Active", checkout.GetProperty("status").GetString());
        Assert.True(checkout.GetProperty("items").GetArrayLength() > 0);
    }

    [Fact]
    public async Task StartCheckout_EmptyCart_ReturnsBadRequest()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await EnsureStoreContextAsync(client);

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.BadRequest, start.StatusCode);
    }

    [Fact]
    public async Task GuestCheckout_CompletesReadyForOrder()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CHK-GUEST-READY", 40m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
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
                Country = "US",
                City = "Los Angeles",
                Address1 = "Street 1",
                PostalCode = "90001"
            }
        });

        var getAfterAddress = await client.GetAsync($"/api/checkout/{checkoutId}");
        using var addressJson = JsonDocument.Parse(await getAfterAddress.Content.ReadAsStreamAsync());
        var shippingOptions = addressJson.RootElement.GetProperty("data").GetProperty("shippingOptions");
        if (shippingOptions.GetArrayLength() > 0)
        {
            var option = shippingOptions[0];
            await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-method", new
            {
                methodId = option.GetProperty("id").GetString(),
                providerSystemName = option.GetProperty("providerSystemName").GetString()
            });
        }

        var validate = await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
        using var validateJson = JsonDocument.Parse(await validate.Content.ReadAsStreamAsync());
        var result = validateJson.RootElement.GetProperty("data");
        Assert.True(result.GetProperty("isReadyForOrder").GetBoolean());
        Assert.Equal("ReadyForOrder", result.GetProperty("checkout").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CustomerCheckout_UsesSavedAddress()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CHK-CUST-1", 18m);

        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, "checkout-customer@example.com", "Password123!");
        var addressResponse = await client.PostAsJsonAsync("/api/customers/me/addresses", new
        {
            Label = "Home",
            FirstName = "Customer",
            LastName = "One",
            Country = "IR",
            City = "Tehran",
            Address1 = "Street 2",
            PostalCode = "9876543210",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });
        Assert.Equal(HttpStatusCode.Created, addressResponse.StatusCode);

        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkout = startJson.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Object, checkout.GetProperty("billingAddress").ValueKind);
        Assert.Equal("Customer", checkout.GetProperty("billingAddress").GetProperty("firstName").GetString());

        var checkoutId = checkout.GetProperty("id").GetInt32();
        var validate = await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
    }

    [Fact]
    public async Task Checkout_IsolatedBetweenGuests()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        await InstallationFlowTests.CompleteInstallationAsync(
            factory.CreateClient(),
            InstallationFlowTests.CreateInMemoryToken());

        using var clientA = CreateStorefrontClient(factory);
        using var clientB = CreateStorefrontClient(factory);

        var offerId = await CreateSimpleProductOfferAsync(clientA, "CHK-ISO-GUEST", 11m);
        await clientA.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await clientA.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var foreignGet = await clientB.GetAsync($"/api/checkout/{checkoutId}");
        Assert.Equal(HttpStatusCode.BadRequest, foreignGet.StatusCode);
    }

    private static async Task EnsureStoreContextAsync(HttpClient client)
    {
        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = "localhost";
        await client.SendAsync(contextRequest);
    }

    [Fact]
    public async Task CartModification_MarksCheckoutRequiresReview()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "CHK-STALE", 22m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
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
        await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);

        var cart = await client.GetAsync("/api/cart");
        using var cartJson = JsonDocument.Parse(await cart.Content.ReadAsStreamAsync());
        var itemId = cartJson.RootElement.GetProperty("data").GetProperty("items")[0].GetProperty("id").GetInt32();
        await client.PutAsJsonAsync($"/api/cart/items/{itemId}", new { Quantity = 2 });

        var get = await client.GetAsync($"/api/checkout/{checkoutId}");
        using var getJson = JsonDocument.Parse(await get.Content.ReadAsStreamAsync());
        Assert.Equal("RequiresReview", getJson.RootElement.GetProperty("data").GetProperty("status").GetString());
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
        bool isActive = true)
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
        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var currencies = await client.GetAsync("/api/currencies");
        using var currenciesJson = JsonDocument.Parse(await currencies.Content.ReadAsStreamAsync());
        var stores = await client.GetAsync("/api/stores");
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        var storeId = storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = "localhost";
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
