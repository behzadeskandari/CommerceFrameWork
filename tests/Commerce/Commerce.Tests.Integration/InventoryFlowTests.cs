using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class InventoryFlowTests
{
    [Fact]
    public async Task Admin_CreateInventory_AdjustStock_ListMovements_Succeeds()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-ADMIN-1", 19m);
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var create = await client.PostAsJsonAsync("/api/admin/inventory", new
        {
            OfferId = offerId,
            TrackInventory = true,
            AllowBackorder = false,
            InitialOnHand = 12
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var inventoryId = createJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        Assert.Equal(12, createJson.RootElement.GetProperty("data").GetProperty("onHand").GetInt32());

        var adjust = await client.PostAsJsonAsync($"/api/admin/inventory/{inventoryId}/adjust", new
        {
            QuantityDelta = -2,
            MovementType = InventoryMovementType.Damage,
            Reason = "Damaged units."
        });
        Assert.Equal(HttpStatusCode.OK, adjust.StatusCode);

        var movements = await client.GetAsync($"/api/admin/inventory/{inventoryId}/movements");
        Assert.Equal(HttpStatusCode.OK, movements.StatusCode);
        using var movementsJson = JsonDocument.Parse(await movements.Content.ReadAsStreamAsync());
        Assert.True(movementsJson.RootElement.GetProperty("data").GetArrayLength() >= 2);
    }

    [Fact]
    public async Task OrderCreation_ReservesInventory_CancellationReleases()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-ORDER-1", 25m);
        await CreateInventoryAsync(client, offerId, onHand: 5);

        var checkoutId = await PrepareReadyGuestCheckoutAsync(client, offerId);
        var create = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);
        var list = await client.GetAsync("/api/admin/inventory?offerId=" + offerId);
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStreamAsync());
        var item = listJson.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(1, item.GetProperty("reserved").GetInt32());
        var inventoryId = item.GetProperty("id").GetInt32();

        using var createOrderJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var orderId = createOrderJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var cancel = await client.PostAsJsonAsync($"/api/admin/orders/{orderId}/cancel", new { Reason = "Test cancel." });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var detail = await client.GetAsync($"/api/admin/inventory/{inventoryId}");
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStreamAsync());
        Assert.Equal(0, detailJson.RootElement.GetProperty("data").GetProperty("reserved").GetInt32());
        Assert.Equal(5, detailJson.RootElement.GetProperty("data").GetProperty("onHand").GetInt32());
    }

    [Fact]
    public async Task CheckoutValidation_FailsWhenInsufficientInventory()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-CHECKOUT-1", 14m);
        var inventoryId = await CreateInventoryAsync(client, offerId, onHand: 1);

        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        var start = await client.PostAsync("/api/checkout", null);
        start.EnsureSuccessStatusCode();
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

        using var adminClient = CreateStorefrontClient(factory);
        await IntegrationAuthHelper.LoginAsAdministratorAsync(adminClient);
        await adminClient.PostAsJsonAsync($"/api/admin/inventory/{inventoryId}/adjust", new
        {
            QuantityDelta = -1,
            MovementType = InventoryMovementType.Correction,
            Reason = "Sold out before checkout."
        });

        var validate = await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);
        Assert.Equal(HttpStatusCode.BadRequest, validate.StatusCode);
    }

    [Fact]
    public async Task ConcurrentReservation_OnlyOneSucceedsWhenOnHandIsOne()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-CONC-1", 11m);
        var inventoryId = await CreateInventoryAsync(client, offerId, onHand: 1);

        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var serviceA = scopeA.ServiceProvider.GetRequiredService<IInventoryReservationService>();
        var serviceB = scopeB.ServiceProvider.GetRequiredService<IInventoryReservationService>();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var taskA = serviceA.ReserveAsync(
            inventoryId,
            1,
            InventoryReferenceType.Order,
            1001,
            expiresAt,
            CancellationToken.None);

        var taskB = serviceB.ReserveAsync(
            inventoryId,
            1,
            InventoryReferenceType.Order,
            1002,
            expiresAt,
            CancellationToken.None);

        var results = await Task.WhenAll(taskA, taskB);
        var successes = results.Count(result => result.IsSuccess);

        Assert.Equal(1, successes);

        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);
        var detail = await client.GetAsync($"/api/admin/inventory/{inventoryId}");
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStreamAsync());
        var data = detailJson.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetProperty("onHand").GetInt32());
        Assert.Equal(1, data.GetProperty("reserved").GetInt32());
        Assert.Equal(0, data.GetProperty("available").GetInt32());
    }

    [Fact]
    public async Task Warehouse_CreateAndTransferStock_Succeeds()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-WH-1", 22m);
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var warehouseA = await client.PostAsJsonAsync("/api/admin/inventory/warehouses", new
        {
            Name = "Warehouse A",
            SystemName = "warehouse-a",
            IsDefault = true,
            DisplayOrder = 0
        });
        Assert.Equal(HttpStatusCode.Created, warehouseA.StatusCode);
        using var warehouseAJson = JsonDocument.Parse(await warehouseA.Content.ReadAsStreamAsync());
        var warehouseAId = warehouseAJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var warehouseB = await client.PostAsJsonAsync("/api/admin/inventory/warehouses", new
        {
            Name = "Warehouse B",
            SystemName = "warehouse-b",
            IsDefault = false,
            DisplayOrder = 1
        });
        Assert.Equal(HttpStatusCode.Created, warehouseB.StatusCode);
        using var warehouseBJson = JsonDocument.Parse(await warehouseB.Content.ReadAsStreamAsync());
        var warehouseBId = warehouseBJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var sourceInventory = await client.PostAsJsonAsync("/api/admin/inventory", new
        {
            OfferId = offerId,
            TrackInventory = true,
            AllowBackorder = false,
            InitialOnHand = 8,
            WarehouseId = warehouseAId
        });
        sourceInventory.EnsureSuccessStatusCode();
        using var sourceJson = JsonDocument.Parse(await sourceInventory.Content.ReadAsStreamAsync());
        var sourceId = sourceJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var destInventory = await client.PostAsJsonAsync("/api/admin/inventory", new
        {
            OfferId = offerId,
            TrackInventory = true,
            AllowBackorder = false,
            InitialOnHand = 0,
            WarehouseId = warehouseBId
        });
        destInventory.EnsureSuccessStatusCode();
        using var destJson = JsonDocument.Parse(await destInventory.Content.ReadAsStreamAsync());
        var destId = destJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var transfer = await client.PostAsJsonAsync("/api/admin/inventory/transfer", new
        {
            SourceInventoryItemId = sourceId,
            DestinationInventoryItemId = destId,
            Quantity = 3,
            Reason = "Replenish secondary warehouse."
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        var sourceDetail = await client.GetAsync($"/api/admin/inventory/{sourceId}");
        using var sourceDetailJson = JsonDocument.Parse(await sourceDetail.Content.ReadAsStreamAsync());
        Assert.Equal(5, sourceDetailJson.RootElement.GetProperty("data").GetProperty("onHand").GetInt32());

        var destDetail = await client.GetAsync($"/api/admin/inventory/{destId}");
        using var destDetailJson = JsonDocument.Parse(await destDetail.Content.ReadAsStreamAsync());
        Assert.Equal(3, destDetailJson.RootElement.GetProperty("data").GetProperty("onHand").GetInt32());
    }

    [Fact]
    public async Task ReceiveIncoming_UpdatesOnHandAndIncoming()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-INCOMING-1", 18m);
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var create = await client.PostAsJsonAsync("/api/admin/inventory", new
        {
            OfferId = offerId,
            TrackInventory = true,
            AllowBackorder = false,
            InitialOnHand = 2,
            InitialIncoming = 5
        });
        create.EnsureSuccessStatusCode();
        using var createJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var inventoryId = createJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        Assert.Equal(5, createJson.RootElement.GetProperty("data").GetProperty("incoming").GetInt32());

        var receive = await client.PostAsJsonAsync($"/api/admin/inventory/{inventoryId}/receive-incoming", new
        {
            Quantity = 4,
            Reason = "PO arrival."
        });
        Assert.Equal(HttpStatusCode.OK, receive.StatusCode);
        using var receiveJson = JsonDocument.Parse(await receive.Content.ReadAsStreamAsync());
        Assert.Equal(6, receiveJson.RootElement.GetProperty("data").GetProperty("onHand").GetInt32());
        Assert.Equal(1, receiveJson.RootElement.GetProperty("data").GetProperty("incoming").GetInt32());
    }

    [Fact]
    public async Task Overselling_PreventedWhenTwoOrdersCompeteForLastUnit()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-OVER-1", 13m);
        await CreateInventoryAsync(client, offerId, onHand: 1);

        var checkoutA = await PrepareReadyGuestCheckoutAsync(client, offerId);
        var checkoutB = await PrepareReadyGuestCheckoutAsync(client, offerId);

        var orderA = await CreateOrderAsync(client, checkoutA, Guid.NewGuid().ToString("N"));
        var orderB = await CreateOrderAsync(client, checkoutB, Guid.NewGuid().ToString("N"));

        var successCount = new[] { orderA.StatusCode, orderB.StatusCode }.Count(code => code == HttpStatusCode.Created);
        Assert.Equal(1, successCount);
    }

    [Fact]
    public async Task NonTrackedOffer_AllowsCheckoutWithoutInventoryItem()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var offerId = await CreateSimpleProductOfferAsync(client, "INV-NOT-TRACKED", 9m);
        var checkoutId = await PrepareReadyGuestCheckoutAsync(client, offerId);
        var create = await CreateOrderAsync(client, checkoutId, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    private static HttpClient CreateStorefrontClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<int> CreateInventoryAsync(HttpClient client, int offerId, int onHand)
    {
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);
        var create = await client.PostAsJsonAsync("/api/admin/inventory", new
        {
            OfferId = offerId,
            TrackInventory = true,
            AllowBackorder = false,
            InitialOnHand = onHand
        });
        create.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static async Task<HttpResponseMessage> CreateOrderAsync(HttpClient client, int checkoutId, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new { CheckoutId = checkoutId })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<int> PrepareReadyGuestCheckoutAsync(HttpClient client, int offerId)
    {
        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var start = await client.PostAsync("/api/checkout", null);
        start.EnsureSuccessStatusCode();
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
        validate.EnsureSuccessStatusCode();
        return checkoutId;
    }

    private static async Task<int> CreateSimpleProductOfferAsync(HttpClient client, string sku, decimal price)
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
            IsActive = true
        });
        using var offerJson = JsonDocument.Parse(await offerResponse.Content.ReadAsStreamAsync());
        return offerJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }
}
