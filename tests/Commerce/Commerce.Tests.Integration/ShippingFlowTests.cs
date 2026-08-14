using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Domain.Enums;
using Commerce.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class ShippingFlowTests
{
    [Fact]
    public async Task AdminShipping_MethodZoneRateCrud_Works()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var stores = await client.GetAsync("/api/stores");
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        var storeId = storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var methodResponse = await client.PostAsJsonAsync("/api/admin/shipping/methods", new
        {
            storeId,
            name = "Express",
            systemName = "express",
            description = "Express shipping",
            providerSystemName = ShippingProviderNames.FlatRate,
            isActive = true,
            displayOrder = 1,
            requiresAddress = true,
            supportsTracking = false,
            estimatedDeliveryDaysMin = 1,
            estimatedDeliveryDaysMax = 2
        });
        Assert.Equal(HttpStatusCode.Created, methodResponse.StatusCode);

        var zoneResponse = await client.PostAsJsonAsync("/api/admin/shipping/zones", new
        {
            storeId,
            name = "United States",
            systemName = "us",
            isDefault = false,
            isActive = true,
            displayOrder = 0,
            countries = new[] { new { countryCode = "US" } },
            states = Array.Empty<object>(),
            postalRules = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.Created, zoneResponse.StatusCode);

        using var methodJson = JsonDocument.Parse(await methodResponse.Content.ReadAsStreamAsync());
        using var zoneJson = JsonDocument.Parse(await zoneResponse.Content.ReadAsStreamAsync());
        var methodId = methodJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        var zoneId = zoneJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var rateResponse = await client.PostAsJsonAsync("/api/admin/shipping/rates", new
        {
            storeId,
            shippingMethodId = methodId,
            shippingZoneId = zoneId,
            currencyCode = "USD",
            rateType = "Flat",
            basePrice = 12.5m,
            pricePerWeightUnit = (decimal?)null,
            freeShippingThreshold = (decimal?)null,
            minOrderSubtotal = (decimal?)null,
            maxOrderSubtotal = (decimal?)null
        });
        Assert.Equal(HttpStatusCode.Created, rateResponse.StatusCode);

        var list = await client.GetAsync("/api/admin/shipping/methods");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task PhysicalProduct_CheckoutReturnsShippingOptions()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateProductOfferAsync(client, "SHIP-PHYS-1", ProductType.Simple, 30m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "ship@example.com" });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "US",
                City = "Los Angeles",
                Address1 = "1 Main St",
                PostalCode = "90001"
            }
        });
        var shippingAddress = await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "US",
                City = "Los Angeles",
                Address1 = "1 Main St",
                PostalCode = "90001"
            }
        });
        Assert.Equal(HttpStatusCode.OK, shippingAddress.StatusCode);

        using var shippingJson = JsonDocument.Parse(await shippingAddress.Content.ReadAsStreamAsync());
        var checkout = shippingJson.RootElement.GetProperty("data");
        Assert.True(checkout.GetProperty("requiresShipping").GetBoolean());
        Assert.True(checkout.GetProperty("shippingOptions").GetArrayLength() > 0);
    }

    [Fact]
    public async Task DigitalOnlyCart_SkipsShipping()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateProductOfferAsync(client, "SHIP-DIG-1", ProductType.Digital, 15m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkout = startJson.RootElement.GetProperty("data");

        Assert.False(checkout.GetProperty("requiresShipping").GetBoolean());
        Assert.Equal(0, checkout.GetProperty("shippingOptions").GetArrayLength());
    }

    [Fact]
    public async Task ShippingSelection_RecalculatesTotals()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateProductOfferAsync(client, "SHIP-SEL-1", ProductType.Simple, 25m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "ship@example.com" });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "US",
                City = "Los Angeles",
                Address1 = "1 Main St",
                PostalCode = "90001"
            }
        });
        var withAddress = await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "US",
                City = "Los Angeles",
                Address1 = "1 Main St",
                PostalCode = "90001"
            }
        });

        using var addressJson = JsonDocument.Parse(await withAddress.Content.ReadAsStreamAsync());
        var option = addressJson.RootElement.GetProperty("data").GetProperty("shippingOptions")[0];
        var methodId = option.GetProperty("id").GetString();
        var provider = option.GetProperty("providerSystemName").GetString();
        var price = option.GetProperty("price").GetDecimal();

        var select = await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-method", new
        {
            methodId,
            providerSystemName = provider
        });
        Assert.Equal(HttpStatusCode.OK, select.StatusCode);

        using var selectJson = JsonDocument.Parse(await select.Content.ReadAsStreamAsync());
        var totals = selectJson.RootElement.GetProperty("data").GetProperty("totals");
        Assert.Equal(price, totals.GetProperty("shippingTotal").GetDecimal());
        Assert.True(totals.GetProperty("grandTotal").GetDecimal() > totals.GetProperty("subtotal").GetDecimal());
    }

    [Fact]
    public async Task AddressChange_InvalidatesShippingSelection()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateProductOfferAsync(client, "SHIP-ADDR-1", ProductType.Simple, 20m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "ship@example.com" });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "US",
                City = "Los Angeles",
                Address1 = "1 Main St",
                PostalCode = "90001"
            }
        });
        var firstAddress = await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "US",
                City = "Los Angeles",
                Address1 = "1 Main St",
                PostalCode = "90001"
            }
        });
        using var firstJson = JsonDocument.Parse(await firstAddress.Content.ReadAsStreamAsync());
        var option = firstJson.RootElement.GetProperty("data").GetProperty("shippingOptions")[0];
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-method", new
        {
            methodId = option.GetProperty("id").GetString(),
            providerSystemName = option.GetProperty("providerSystemName").GetString()
        });

        var secondAddress = await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "US",
                City = "San Francisco",
                Address1 = "2 Market St",
                PostalCode = "94105"
            }
        });
        using var secondJson = JsonDocument.Parse(await secondAddress.Content.ReadAsStreamAsync());
        var checkout = secondJson.RootElement.GetProperty("data");
        Assert.True(string.IsNullOrEmpty(checkout.GetProperty("selectedShippingMethodId").GetString()));
    }

    [Fact]
    public async Task MixedCart_RequiresShippingAndCalculatesOnPhysicalLinesOnly()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var physicalOffer = await CreateProductOfferAsync(client, "SHIP-MIX-PHYS", ProductType.Simple, 20m);
        var digitalOffer = await CreateProductOfferAsync(client, "SHIP-MIX-DIG", ProductType.Digital, 10m);

        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = physicalOffer, Quantity = 1 });
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = digitalOffer, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkout = startJson.RootElement.GetProperty("data");

        Assert.True(checkout.GetProperty("requiresShipping").GetBoolean());
    }

    [Fact]
    public async Task InvalidAddress_ReturnsNoShippingOptionsWhenZoneMissing()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateProductOfferAsync(client, "SHIP-INVALID-1", ProductType.Simple, 18m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "ship@example.com" });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "IR",
                City = "Tehran",
                Address1 = "1 Street",
                PostalCode = "1234567890"
            }
        });

        var shippingAddress = await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/shipping-address", new
        {
            Address = new
            {
                FirstName = "Ship",
                LastName = "Test",
                Country = "IR",
                City = "Tehran",
                Address1 = "1 Street",
                PostalCode = "1234567890"
            }
        });

        using var shippingJson = JsonDocument.Parse(await shippingAddress.Content.ReadAsStreamAsync());
        var options = shippingJson.RootElement.GetProperty("data").GetProperty("shippingOptions");
        Assert.Equal(0, options.GetArrayLength());
    }

    [Fact]
    public async Task PickupMethod_AllowsCheckoutWithoutShippingAddress()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var stores = await client.GetAsync("/api/stores");
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        var storeId = storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var methodResponse = await client.PostAsJsonAsync("/api/admin/shipping/methods", new
        {
            storeId,
            name = "Store Pickup",
            systemName = "pickup",
            description = "Collect in store",
            providerSystemName = ShippingProviderNames.Pickup,
            isActive = true,
            displayOrder = 0,
            requiresAddress = false,
            supportsTracking = false
        });
        methodResponse.EnsureSuccessStatusCode();

        var offerId = await CreateProductOfferAsync(client, "SHIP-PICKUP-1", ProductType.Simple, 22m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "pickup@example.com" });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new
        {
            Address = new
            {
                FirstName = "Pickup",
                LastName = "Customer",
                Country = "US",
                City = "LA",
                Address1 = "1 Main",
                PostalCode = "90001"
            }
        });

        var checkout = await client.GetAsync($"/api/checkout/{checkoutId}");
        using var checkoutJson = JsonDocument.Parse(await checkout.Content.ReadAsStreamAsync());
        var options = checkoutJson.RootElement.GetProperty("data").GetProperty("shippingOptions");
        Assert.True(options.GetArrayLength() > 0);
        Assert.False(options[0].GetProperty("requiresAddress").GetBoolean());
    }

    private static HttpClient CreateStorefrontClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task<int> CreateProductOfferAsync(
        HttpClient client,
        string sku,
        ProductType productType,
        decimal price)
    {
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var productResponse = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            Name = sku,
            Sku = sku,
            ProductType = productType,
            Published = true,
            IsVisible = true,
            IsAvailable = true
        });
        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var stores = await client.GetAsync("/api/stores");
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        var storeId = storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var currencies = await client.GetAsync("/api/currencies");
        using var currenciesJson = JsonDocument.Parse(await currencies.Content.ReadAsStreamAsync());

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
            IsActive = true
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
