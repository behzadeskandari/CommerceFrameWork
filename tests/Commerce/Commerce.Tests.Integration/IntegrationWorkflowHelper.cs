using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Commerce.Tests.Integration;

internal static class IntegrationWorkflowHelper
{
    internal static HttpClient CreateStorefrontClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    internal static async Task<(WebApplicationFactory<Program> Factory, HttpClient Client)> CreateInstalledStorefrontAsync()
    {
        var factory = InstallationFlowTests.CreateFactory();
        var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await EnsureManualPaymentPluginAsync(client);
        return (factory, client);
    }

    internal static async Task EnsureManualPaymentPluginAsync(HttpClient client)
    {
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var detail = await client.GetAsync("/api/admin/plugins/Payment.Manual");
        if (detail.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        using var json = JsonDocument.Parse(await detail.Content.ReadAsStreamAsync());
        var plugin = json.RootElement.GetProperty("data");

        if (!plugin.GetProperty("isInstalled").GetBoolean())
        {
            var install = await client.PostAsync("/api/admin/plugins/Payment.Manual/install", null);
            install.EnsureSuccessStatusCode();
        }

        if (!plugin.GetProperty("isEnabled").GetBoolean())
        {
            var enable = await client.PostAsync("/api/admin/plugins/Payment.Manual/enable", null);
            enable.EnsureSuccessStatusCode();
        }

        await LogoutAsync(client);
    }

    internal static async Task LogoutAsync(HttpClient client)
    {
        var logout = await client.PostAsync("/api/customers/logout", null);
        if (logout.StatusCode == HttpStatusCode.Unauthorized)
        {
            await client.PostAsync("/api/auth/logout", null);
        }
    }

    internal static async Task<(int OfferId, int ProductId)> CreateProductWithOfferAsync(
        HttpClient client,
        string sku,
        ProductType productType,
        decimal price,
        bool logoutAfter = true)
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
        productResponse.EnsureSuccessStatusCode();

        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var storeId = await GetDefaultStoreIdAsync(client);
        var (currencyId, currencyCode) = await GetStoreCurrencyAsync(client);

        var offerResponse = await client.PostAsJsonAsync("/api/catalog/offers", new
        {
            ProductId = productId,
            StoreId = storeId,
            CurrencyId = currencyId,
            CurrencyCode = currencyCode,
            Price = price,
            IsActive = true
        });
        offerResponse.EnsureSuccessStatusCode();

        using var offerJson = JsonDocument.Parse(await offerResponse.Content.ReadAsStreamAsync());
        var offerId = offerJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        if (logoutAfter)
        {
            await LogoutAsync(client);
        }

        return (offerId, productId);
    }

    internal static async Task<int> CreateProductOfferAsync(
        HttpClient client,
        string sku,
        ProductType productType,
        decimal price,
        bool logoutAfter = true)
    {
        var (offerId, _) = await CreateProductWithOfferAsync(client, sku, productType, price, logoutAfter);
        return offerId;
    }

    internal static async Task<int> GetDefaultStoreIdAsync(HttpClient client)
    {
        var stores = await client.GetAsync("/api/stores");
        stores.EnsureSuccessStatusCode();
        using var storesJson = JsonDocument.Parse(await stores.Content.ReadAsStreamAsync());
        return storesJson.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();
    }

    internal static async Task<(int CurrencyId, string CurrencyCode)> GetStoreCurrencyAsync(HttpClient client)
    {
        var currencies = await client.GetAsync("/api/currencies");
        currencies.EnsureSuccessStatusCode();
        using var currenciesJson = JsonDocument.Parse(await currencies.Content.ReadAsStreamAsync());

        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = "localhost";
        var contextResponse = await client.SendAsync(contextRequest);
        contextResponse.EnsureSuccessStatusCode();

        using var contextJson = JsonDocument.Parse(await contextResponse.Content.ReadAsStreamAsync());
        var currencyId = contextJson.RootElement.GetProperty("data").GetProperty("currencyId").GetInt32();
        var currencyCode = currenciesJson.RootElement.GetProperty("data")
            .EnumerateArray()
            .First(x => x.GetProperty("id").GetInt32() == currencyId)
            .GetProperty("code")
            .GetString()!;

        return (currencyId, currencyCode);
    }

    internal static async Task<int> PrepareReadyCustomerCheckoutAsync(
        HttpClient client,
        int offerId,
        string paymentMethodSystemName = "bank-transfer")
    {
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        start.EnsureSuccessStatusCode();
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStreamAsync());
        var checkoutId = startJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var methods = await client.GetAsync($"/api/checkout/{checkoutId}/payment-methods");
        methods.EnsureSuccessStatusCode();
        using var methodsJson = JsonDocument.Parse(await methods.Content.ReadAsStreamAsync());
        var method = methodsJson.RootElement.GetProperty("data")
            .EnumerateArray()
            .First(m => string.Equals(
                m.GetProperty("systemName").GetString(),
                paymentMethodSystemName,
                StringComparison.OrdinalIgnoreCase));

        var methodId = method.GetProperty("id").GetString() ?? method.GetProperty("id").GetRawText();
        var systemName = method.GetProperty("systemName").GetString()!;

        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/payment-method", new
        {
            methodId,
            systemName
        });

        var validate = await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);
        validate.EnsureSuccessStatusCode();
        return checkoutId;
    }

    internal static async Task<(int OrderId, string OrderNumber)> CreateOrderAsync(
        HttpClient client,
        int checkoutId,
        string? idempotencyKey = null)
    {
        var key = idempotencyKey ?? Guid.NewGuid().ToString("N");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new { CheckoutId = checkoutId })
        };
        request.Headers.Add("Idempotency-Key", key);

        var create = await client.SendAsync(request);
        create.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var data = json.RootElement.GetProperty("data");
        return (
            data.GetProperty("id").GetInt32(),
            data.GetProperty("orderNumber").GetString()!);
    }

    internal static async Task<int> CreatePaymentAsync(HttpClient client, int orderId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new { OrderId = orderId, PaymentMethodSystemName = "bank-transfer" })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("data").GetProperty("paymentId").GetInt32();
    }

    internal static async Task CapturePaymentAsync(HttpClient client, int paymentId)
    {
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);
        var capture = await client.PostAsync($"/api/admin/payments/{paymentId}/capture", null);
        capture.EnsureSuccessStatusCode();
        await LogoutAsync(client);
    }

    internal static async Task FulfillOrderAsync(HttpClient client, int orderId)
    {
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var orderResponse = await client.GetAsync($"/api/admin/orders/{orderId}");
        orderResponse.EnsureSuccessStatusCode();
        using var orderJson = JsonDocument.Parse(await orderResponse.Content.ReadAsStreamAsync());
        var order = orderJson.RootElement.GetProperty("data");
        var items = order.GetProperty("items")
            .EnumerateArray()
            .Select(x => new
            {
                OrderItemId = x.GetProperty("id").GetInt32(),
                OfferId = x.GetProperty("offerId").GetInt32(),
                ProductId = x.GetProperty("productId").GetInt32(),
                Quantity = x.GetProperty("quantity").GetInt32()
            })
            .ToArray();

        var confirm = await client.PostAsJsonAsync($"/api/admin/orders/{orderId}/confirm", new { Note = "Verified." });
        confirm.EnsureSuccessStatusCode();

        var processing = await client.PostAsync($"/api/admin/orders/{orderId}/processing", null);
        processing.EnsureSuccessStatusCode();

        var shipment = await client.PostAsJsonAsync("/api/admin/shipping/shipments", new
        {
            orderId,
            notes = "Phase 45 verification shipment",
            items = items.Select(x => new
            {
                orderItemId = x.OrderItemId,
                offerId = x.OfferId,
                productId = x.ProductId,
                quantity = x.Quantity
            })
        });
        shipment.EnsureSuccessStatusCode();
        using var shipmentJson = JsonDocument.Parse(await shipment.Content.ReadAsStreamAsync());
        var shipmentId = shipmentJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var tracking = await client.PutAsJsonAsync($"/api/admin/shipping/shipments/{shipmentId}/tracking", new
        {
            trackingNumber = $"TRK-{orderId}",
            trackingUrl = $"https://track.test/{orderId}",
            carrierName = "Test Carrier"
        });
        tracking.EnsureSuccessStatusCode();

        var shipped = await client.PostAsync($"/api/admin/shipping/shipments/{shipmentId}/ship", null);
        shipped.EnsureSuccessStatusCode();

        var delivered = await client.PostAsync($"/api/admin/shipping/shipments/{shipmentId}/deliver", null);
        delivered.EnsureSuccessStatusCode();

        var complete = await client.PostAsJsonAsync($"/api/admin/orders/{orderId}/complete", new { Note = "Delivered." });
        complete.EnsureSuccessStatusCode();

        await LogoutAsync(client);
    }

    internal static async Task<int> UploadMediaAsync(HttpClient client, byte[] content, string fileName, string contentType, bool isPublic)
    {
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(isPublic ? "true" : "false"), "isPublic");

        var upload = await client.PostAsync("/api/media/upload", form);
        upload.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await upload.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }
}
