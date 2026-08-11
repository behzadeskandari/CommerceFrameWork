using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class OrderFlowTests
{
    [Fact]
    public async Task CreateOrder_GuestFromReadyCheckout_Succeeds()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var checkoutId = await PrepareReadyGuestCheckoutAsync(client, "ORD-GUEST-1", 35m);
        var create = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        var createBody = await create.Content.ReadAsStringAsync();
        Assert.True(create.StatusCode == HttpStatusCode.Created, $"Status: {create.StatusCode}, Body: {createBody}");

        using var createJson = JsonDocument.Parse(createBody);
        var order = createJson.RootElement.GetProperty("data");
        var orderNumber = order.GetProperty("orderNumber").GetString();
        var guestAccessToken = order.GetProperty("guestAccessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(orderNumber));
        Assert.Matches(@"^ORD-\d{4}-\d{6}$", orderNumber!);
        Assert.False(string.IsNullOrWhiteSpace(guestAccessToken));

        var get = await client.GetAsync($"/api/orders/by-number/{orderNumber}?accessToken={guestAccessToken}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var getJson = JsonDocument.Parse(await get.Content.ReadAsStreamAsync());
        Assert.Equal("Pending", getJson.RootElement.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateOrder_CustomerFromReadyCheckout_Succeeds()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "ORD-CUST-1", 28m);
        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, "order-customer@example.com", "Password123!");
        await client.PostAsJsonAsync("/api/customers/me/addresses", new
        {
            Label = "Home",
            FirstName = "Order",
            LastName = "Customer",
            Country = "IR",
            City = "Tehran",
            Address1 = "Street 3",
            PostalCode = "1111111111",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });

        var checkoutId = await PrepareReadyCustomerCheckoutAsync(client, offerId);
        var create = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var createJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var orderId = createJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        Assert.Equal(JsonValueKind.Null, createJson.RootElement.GetProperty("data").GetProperty("guestAccessToken").ValueKind);

        var get = await client.GetAsync($"/api/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var getJson = JsonDocument.Parse(await get.Content.ReadAsStreamAsync());
        var detail = getJson.RootElement.GetProperty("data");
        Assert.Equal("order-customer@example.com", detail.GetProperty("customer").GetProperty("email").GetString());
        Assert.False(detail.GetProperty("customer").GetProperty("isGuest").GetBoolean());
    }

    [Fact]
    public async Task CreateOrder_NonReadyCheckout_ReturnsBadRequest()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "ORD-NOT-READY", 15m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var create = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_SameIdempotencyKey_ReturnsSameOrder()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var checkoutId = await PrepareReadyGuestCheckoutAsync(client, "ORD-IDEM-1", 22m);
        var idempotencyKey = Guid.NewGuid().ToString("N");

        var first = await CreateOrderAsync(client, checkoutId, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStreamAsync());
        var firstId = firstJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        var firstNumber = firstJson.RootElement.GetProperty("data").GetProperty("orderNumber").GetString();

        var second = await CreateOrderAsync(client, checkoutId, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStreamAsync());
        var secondId = secondJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        var secondNumber = secondJson.RootElement.GetProperty("data").GetProperty("orderNumber").GetString();

        Assert.Equal(firstId, secondId);
        Assert.Equal(firstNumber, secondNumber);
    }

    [Fact]
    public async Task CreateOrder_DuplicateCheckout_ReturnsConflict()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var checkoutId = await PrepareReadyGuestCheckoutAsync(client, "ORD-DUP-1", 19m);

        var first = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Order_IsolatedBetweenCustomers()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        await InstallationFlowTests.CompleteInstallationAsync(
            factory.CreateClient(),
            InstallationFlowTests.CreateInMemoryToken());

        using var clientA = CreateStorefrontClient(factory);
        using var clientB = CreateStorefrontClient(factory);

        var offerId = await CreateSimpleProductOfferAsync(clientA, "ORD-ISO-1", 31m);
        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(clientA, "order-owner@example.com", "Password123!");
        await clientA.PostAsJsonAsync("/api/customers/me/addresses", new
        {
            Label = "Home",
            FirstName = "Owner",
            LastName = "Customer",
            Country = "IR",
            City = "Tehran",
            Address1 = "Street 4",
            PostalCode = "2222222222",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });

        var checkoutId = await PrepareReadyCustomerCheckoutAsync(clientA, offerId);
        var create = await CreateOrderAsync(clientA, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var orderId = createJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(clientB, "order-other@example.com", "Password123!");
        var foreignGet = await clientB.GetAsync($"/api/orders/{orderId}");
        Assert.Equal(HttpStatusCode.BadRequest, foreignGet.StatusCode);
    }

    [Fact]
    public async Task AdminOrders_ListAndGet_WorkAfterLogin()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var checkoutId = await PrepareReadyGuestCheckoutAsync(client, "ORD-ADMIN-1", 44m);
        var create = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var orderId = createJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var list = await client.GetAsync("/api/admin/orders");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStreamAsync());
        var items = listJson.RootElement.GetProperty("data").GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);

        var get = await client.GetAsync($"/api/admin/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var getJson = JsonDocument.Parse(await get.Content.ReadAsStreamAsync());
        Assert.Equal(orderId, getJson.RootElement.GetProperty("data").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task CancelOrder_Customer_SucceedsAndRejectsRepeat()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "ORD-CANCEL-1", 26m);
        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, "order-cancel@example.com", "Password123!");
        await client.PostAsJsonAsync("/api/customers/me/addresses", new
        {
            Label = "Home",
            FirstName = "Cancel",
            LastName = "Customer",
            Country = "IR",
            City = "Tehran",
            Address1 = "Street 5",
            PostalCode = "3333333333",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });

        var checkoutId = await PrepareReadyCustomerCheckoutAsync(client, offerId);
        var create = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var orderId = createJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var cancel = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { Reason = "Changed mind." });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        using var cancelJson = JsonDocument.Parse(await cancel.Content.ReadAsStreamAsync());
        Assert.Equal("Cancelled", cancelJson.RootElement.GetProperty("data").GetProperty("status").GetString());

        var repeatCancel = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { Reason = "Again." });
        Assert.Equal(HttpStatusCode.BadRequest, repeatCancel.StatusCode);
    }

    private static HttpClient CreateStorefrontClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task<HttpResponseMessage> CreateOrderAsync(
        HttpClient client,
        int checkoutId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new { CheckoutId = checkoutId })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<int> PrepareReadyGuestCheckoutAsync(
        HttpClient client,
        string sku,
        decimal price)
    {
        var offerId = await CreateSimpleProductOfferAsync(client, sku, price);
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
                Country = "IR",
                City = "Tehran",
                Address1 = "Street 1",
                PostalCode = "1234567890"
            }
        });

        var validate = await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
        return checkoutId;
    }

    private static async Task<int> PrepareReadyCustomerCheckoutAsync(
        HttpClient client,
        int offerId)
    {
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var validate = await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
        return checkoutId;
    }

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
