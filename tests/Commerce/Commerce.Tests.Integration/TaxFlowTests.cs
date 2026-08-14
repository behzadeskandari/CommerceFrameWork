using System.Net;

using System.Net.Http.Json;

using System.Text.Json;

using Commerce.Catalog.Domain.Enums;

using Commerce.Tests.Integration;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;



namespace Commerce.Tests.Integration;



public sealed class TaxFlowTests

{

    [Fact]

    public async Task AdminTax_CategoryZoneRateCrud_Works()

    {

        await using var factory = InstallationFlowTests.CreateFactory();

        using var client = CreateStorefrontClient(factory);

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);



        var stores = await client.GetAsync("/api/stores");

        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());

        var storeId = storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();



        var categoryResponse = await client.PostAsJsonAsync("/api/admin/tax/categories", new

        {

            storeId,

            name = "Reduced",

            systemName = "reduced",

            description = "Reduced rate goods",

            isExempt = false,

            isActive = true,

            displayOrder = 1

        });

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);



        var zoneResponse = await client.PostAsJsonAsync("/api/admin/tax/zones", new

        {

            storeId,

            name = "United States",

            systemName = "us-tax",

            isDefault = false,

            isActive = true,

            displayOrder = 0,

            countries = new[] { new { countryCode = "US" } },

            states = Array.Empty<object>(),

            postalRules = Array.Empty<object>()

        });

        Assert.Equal(HttpStatusCode.Created, zoneResponse.StatusCode);



        using var categoryJson = JsonDocument.Parse(await categoryResponse.Content.ReadAsStreamAsync());

        using var zoneJson = JsonDocument.Parse(await zoneResponse.Content.ReadAsStreamAsync());

        var categoryId = categoryJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var zoneId = zoneJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();



        var rateResponse = await client.PostAsJsonAsync("/api/admin/tax/rates", new

        {

            storeId,

            taxCategoryId = categoryId,

            taxZoneId = zoneId,

            rateType = "Percentage",

            percentage = 8.25m,

            taxShipping = true,

            priority = 0,

            effectiveFromUtc = (DateTime?)null,

            effectiveToUtc = (DateTime?)null

        });

        Assert.Equal(HttpStatusCode.Created, rateResponse.StatusCode);



        var list = await client.GetAsync("/api/admin/tax/categories");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

    }



    [Fact]

    public async Task Checkout_WithBillingAddress_ReturnsTaxTotals()

    {

        await using var factory = InstallationFlowTests.CreateFactory();

        using var client = CreateStorefrontClient(factory);

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());



        var offerId = await CreateProductOfferAsync(client, "TAX-PHYS-1", ProductType.Simple, 50m);

        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });



        var start = await client.PostAsync("/api/checkout", null);

        Assert.Equal(HttpStatusCode.OK, start.StatusCode);

        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());

        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();



        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "tax@example.com" });

        var billing = await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new

        {

            Address = new

            {

                FirstName = "Tax",

                LastName = "Test",

                Country = "US",

                City = "Los Angeles",

                Address1 = "1 Main St",

                PostalCode = "90001"

            }

        });

        Assert.Equal(HttpStatusCode.OK, billing.StatusCode);



        using var billingJson = JsonDocument.Parse(await billing.Content.ReadAsStreamAsync());

        var totals = billingJson.RootElement.GetProperty("data").GetProperty("totals");

        var taxTotal = totals.GetProperty("taxTotal").GetDecimal();

        var productTaxTotal = totals.GetProperty("productTaxTotal").GetDecimal();



        Assert.True(taxTotal > 0m);

        Assert.True(productTaxTotal > 0m);

        Assert.True(totals.GetProperty("taxLines").GetArrayLength() > 0);

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



        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");

        contextRequest.Headers.Host = "localhost";

        var contextResponse = await client.SendAsync(contextRequest);

        using var contextJson = JsonDocument.Parse(await contextResponse.Content.ReadAsStreamAsync());

        var contextCurrencyId = contextJson.RootElement.GetProperty("data").GetProperty("currencyId").GetInt32();



        var offerResponse = await client.PostAsJsonAsync("/api/catalog/offers", new

        {

            ProductId = productId,

            StoreId = storeId,

            CurrencyId = contextCurrencyId,

            Price = price,

            Published = true

        });

        using var offerJson = JsonDocument.Parse(await offerResponse.Content.ReadAsStreamAsync());

        return offerJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

    }

}


