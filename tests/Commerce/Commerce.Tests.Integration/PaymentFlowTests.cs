using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Commerce.Payments.Contracts.Payments;
using Commerce.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class PaymentFlowTests
{
    [Fact]
    public async Task AdminPaymentMethods_Crud_Works()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var stores = await client.GetAsync("/api/stores");
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        var storeId = storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var createResponse = await client.PostAsJsonAsync("/api/admin/payment-methods", new
        {
            storeId,
            name = "Test Bank",
            systemName = "test-bank",
            providerSystemName = PaymentProviderNames.Manual,
            displayName = "Test Bank Transfer",
            isActive = true,
            displayOrder = 5,
            requiresRedirect = false,
            supportsGuest = true,
            supportsFreeOrders = false
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var list = await client.GetAsync("/api/admin/payment-methods");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task SeededPaymentMethods_AppearInCheckout()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateProductOfferAsync(client, "PAY-1", ProductType.Digital, 25m);
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);

        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkout = startJson.RootElement.GetProperty("data");
        Assert.True(checkout.GetProperty("paymentMethods").GetArrayLength() > 0);
    }

    private static HttpClient CreateStorefrontClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
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
        return offerJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }
}
