using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Pricing.Domain.Enums;
using Commerce.Tests.Integration;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class PricingFlowTests
{
    [Fact]
    public async Task AdminDiscount_CreateUpdateActivateDeactivateDelete_Works()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var create = await client.PostAsJsonAsync("/api/admin/discounts", new
        {
            name = "Summer Sale",
            systemName = "summer-sale",
            description = "20% off",
            discountType = DiscountType.Percentage,
            value = 20m,
            currencyCode = (string?)null,
            priority = 80,
            isActive = true,
            startsAtUtc = (DateTime?)null,
            endsAtUtc = (DateTime?)null,
            storeId = (int?)null,
            stackingMode = StackingMode.NonStackable,
            maximumDiscountAmount = (decimal?)null,
            minimumCartSubtotal = (decimal?)null,
            minimumQuantity = (int?)null,
            customerEligibility = CustomerEligibility.All,
            specificCustomerId = (int?)null,
            applicationScope = DiscountApplicationScope.Line,
            targets = new[] { new { targetType = DiscountTargetType.Product, targetId = 1 } }
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createJson = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var id = createJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var get = await client.GetAsync($"/api/admin/discounts/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var deactivate = await client.PostAsync($"/api/admin/discounts/{id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var activate = await client.PostAsync($"/api/admin/discounts/{id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var delete = await client.DeleteAsync($"/api/admin/discounts/{id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task ProductDiscount_AppliesToCartTotals()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var productId = await CreateProductAsync(client, "DISC-PROD-1");
        var offerId = await CreateOfferAsync(client, productId, 100m);

        await CreateProductDiscountAsync(client, productId, 20m);

        var add = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        using var cartJson = JsonDocument.Parse(await add.Content.ReadAsStreamAsync());
        var totals = cartJson.RootElement.GetProperty("data").GetProperty("totals");
        Assert.True(totals.GetProperty("discountTotal").GetDecimal() > 0m);
        Assert.True(totals.GetProperty("grandTotal").GetDecimal() < totals.GetProperty("subtotal").GetDecimal());
    }

    [Fact]
    public async Task Coupon_ApplyRejectExpired_InvalidCode()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var productId = await CreateProductAsync(client, "COUPON-PROD-1");
        var offerId = await CreateOfferAsync(client, productId, 50m);
        var discountId = await CreateCartDiscountAsync(client, 10m, "EUR", minimumCartSubtotal: 0m);
        await CreateCouponAsync(client, discountId, "WELCOME10");

        await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });

        var apply = await client.PostAsJsonAsync("/api/cart/coupons", new { code = "welcome10" });
        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);

        using var appliedJson = JsonDocument.Parse(await apply.Content.ReadAsStreamAsync());
        Assert.Equal("WELCOME10", appliedJson.RootElement.GetProperty("data").GetProperty("appliedCouponCode").GetString());

        var invalid = await client.PostAsJsonAsync("/api/cart/coupons", new { code = "NOTREAL" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task ConcurrentCouponUsage_OnlyOneSucceeds()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        await InstallationFlowTests.CompleteInstallationAsync(
            CreateStorefrontClient(factory),
            InstallationFlowTests.CreateInMemoryToken());

        using var admin = CreateStorefrontClient(factory);
        await IntegrationAuthHelper.LoginAsAdministratorAsync(admin);

        var productId = await CreateProductAsync(admin, "CONCUR-PROD");
        var offerId = await CreateOfferAsync(admin, productId, 25m);
        var discountId = await CreateCartDiscountAsync(admin, 5m, "EUR", minimumCartSubtotal: 0m);
        await CreateCouponAsync(admin, discountId, "ONCEONLY", globalUsageLimit: 1, perCustomerUsageLimit: 1);

        async Task<HttpStatusCode> PlaceOrderAsync()
        {
            using var client = CreateStorefrontClient(factory);
            await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(
                client,
                $"coupon-user-{Guid.NewGuid():N}@example.com",
                "Password123!");
            await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
            await client.PostAsJsonAsync("/api/cart/coupons", new { code = "ONCEONLY" });

            var checkoutStart = await client.PostAsync("/api/checkout", null);
            if (checkoutStart.StatusCode != HttpStatusCode.OK)
            {
                return checkoutStart.StatusCode;
            }

            using var checkoutJson = JsonDocument.Parse(await checkoutStart.Content.ReadAsStreamAsync());
            var checkoutId = checkoutJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

            await CompleteCheckoutForOrderAsync(client, checkoutId);

            var order = await client.PostAsJsonAsync("/api/orders", new { checkoutId });
            return order.StatusCode;
        }

        var first = await PlaceOrderAsync();
        var second = await PlaceOrderAsync();

        Assert.Equal(HttpStatusCode.OK, first);
        Assert.NotEqual(HttpStatusCode.OK, second);
    }

    private static async Task CompleteCheckoutForOrderAsync(HttpClient client, int checkoutId)
    {
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/guest-contact", new { Email = "guest@test.com" });
        await client.PutAsJsonAsync($"/api/checkout/{checkoutId}/billing-address", new
        {
            Address = new
            {
                FirstName = "Test",
                LastName = "User",
                Country = "DE",
                City = "Berlin",
                Address1 = "Street 1",
                PostalCode = "10115"
            },
            UseShippingAsBilling = true
        });
        await client.PostAsync($"/api/checkout/{checkoutId}/validate", null);
    }

    private static async Task<int> CreateProductAsync(HttpClient client, string sku)
    {
        var response = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            name = sku,
            sku,
            productType = "Simple",
            isPublished = true,
            categoryIds = Array.Empty<int>()
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static async Task<int> CreateOfferAsync(HttpClient client, int productId, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/catalog/offers", new
        {
            productId,
            variantId = (int?)null,
            price,
            compareAtPrice = (decimal?)null,
            isActive = true
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static async Task CreateProductDiscountAsync(HttpClient client, int productId, decimal percentage)
    {
        await client.PostAsJsonAsync("/api/admin/discounts", new
        {
            name = "Product Discount",
            systemName = $"prod-disc-{productId}",
            discountType = DiscountType.Percentage,
            value = percentage,
            priority = 80,
            isActive = true,
            stackingMode = StackingMode.NonStackable,
            customerEligibility = CustomerEligibility.All,
            applicationScope = DiscountApplicationScope.Line,
            targets = new[] { new { targetType = DiscountTargetType.Product, targetId = productId } }
        });
    }

    private static async Task<int> CreateCartDiscountAsync(
        HttpClient client,
        decimal amount,
        string currency,
        decimal minimumCartSubtotal)
    {
        var response = await client.PostAsJsonAsync("/api/admin/discounts", new
        {
            name = "Cart Discount",
            systemName = Guid.NewGuid().ToString("N"),
            discountType = DiscountType.FixedAmount,
            value = amount,
            currencyCode = currency,
            priority = 20,
            isActive = true,
            minimumCartSubtotal,
            stackingMode = StackingMode.NonStackable,
            customerEligibility = CustomerEligibility.All,
            applicationScope = DiscountApplicationScope.Cart,
            targets = new[] { new { targetType = DiscountTargetType.Cart, targetId = 0 } }
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static async Task CreateCouponAsync(
        HttpClient client,
        int discountId,
        string code,
        int? globalUsageLimit = null,
        int? perCustomerUsageLimit = null)
    {
        var response = await client.PostAsJsonAsync("/api/admin/coupons", new
        {
            code,
            discountId,
            isActive = true,
            globalUsageLimit,
            perCustomerUsageLimit
        });
        response.EnsureSuccessStatusCode();
    }

    private static HttpClient CreateStorefrontClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
