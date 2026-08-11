using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Commerce.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class Catalog20FlowTests
{
    [Fact]
    public async Task VariantProduct_OfferAndPriceResolution_WorkForStoreContext()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var colorAttr = await client.PostAsJsonAsync("/api/catalog/attributes", new
        {
            Name = "Color",
            Code = "color",
            AttributeType = AttributeType.Option,
            DisplayOrder = 0,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, colorAttr.StatusCode);
        using var colorJson = JsonDocument.Parse(await colorAttr.Content.ReadAsStreamAsync());
        var colorId = colorJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var redOption = await client.PostAsJsonAsync($"/api/catalog/attributes/{colorId}/options", new
        {
            Value = "Red",
            DisplayOrder = 0,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, redOption.StatusCode);
        using var redJson = JsonDocument.Parse(await redOption.Content.ReadAsStreamAsync());
        var redOptionId = redJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var productResponse = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            Name = "Variant Shirt",
            Sku = "SHIRT-VAR-1",
            ProductType = ProductType.Variant,
            Published = true,
            IsVisible = true,
            IsAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var assignResponse = await client.PostAsync($"/api/catalog/attributes/products/{productId}/{colorId}", null);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var variantResponse = await client.PostAsJsonAsync($"/api/catalog/products/{productId}/variants", new
        {
            Sku = "SHIRT-VAR-1-RED",
            Name = "Red",
            AttributeOptionIds = new[] { redOptionId },
            IsActive = true,
            IsDefault = true
        });
        Assert.Equal(HttpStatusCode.Created, variantResponse.StatusCode);
        using var variantJson = JsonDocument.Parse(await variantResponse.Content.ReadAsStreamAsync());
        var variantId = variantJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var currencies = await client.GetAsync("/api/currencies");
        using var currenciesJson = JsonDocument.Parse(await currencies.Content.ReadAsStreamAsync());
        var stores = await client.GetAsync("/api/stores");
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        var storeId = storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = "localhost";
        var contextResponse = await client.SendAsync(contextRequest);
        Assert.Equal(HttpStatusCode.OK, contextResponse.StatusCode);
        using var contextJson = JsonDocument.Parse(await contextResponse.Content.ReadAsStreamAsync());
        var contextCurrencyId = contextJson.RootElement.GetProperty("data").GetProperty("currencyId").GetInt32();

        var offerResponse = await client.PostAsJsonAsync("/api/catalog/offers", new
        {
            ProductId = productId,
            VariantId = variantId,
            StoreId = storeId,
            CurrencyId = contextCurrencyId,
            CurrencyCode = currenciesJson.RootElement.GetProperty("data")
                .EnumerateArray()
                .First(x => x.GetProperty("id").GetInt32() == contextCurrencyId)
                .GetProperty("code")
                .GetString(),
            Price = 29.99m,
            CompareAtPrice = 39.99m,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);

        var priceResponse = await client.GetAsync($"/api/catalog/pricing/variants/{variantId}");
        Assert.Equal(HttpStatusCode.OK, priceResponse.StatusCode);
        using var priceJson = JsonDocument.Parse(await priceResponse.Content.ReadAsStreamAsync());
        Assert.Equal(variantId, priceJson.RootElement.GetProperty("data").GetProperty("variantId").GetInt32());

        var storefrontResponse = await client.GetAsync($"/api/catalog/storefront/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, storefrontResponse.StatusCode);
    }

    [Fact]
    public async Task UnpublishedProduct_NotVisibleOnStorefront()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var productResponse = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            Name = "Draft Product",
            Sku = "DRAFT-001",
            ProductType = ProductType.Simple,
            Published = false,
            IsVisible = false,
            IsAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var storefrontResponse = await client.GetAsync($"/api/catalog/storefront/products/{productId}");
        Assert.Equal(HttpStatusCode.NotFound, storefrontResponse.StatusCode);
    }
}
