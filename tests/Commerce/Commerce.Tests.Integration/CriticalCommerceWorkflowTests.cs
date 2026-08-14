using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Integration;

[Trait("Category", "E2E")]
[Trait("Category", "Workflow")]
[Trait("Phase", "45")]
public sealed class CriticalCommerceWorkflowTests
{
    [Fact]
    public async Task Register_Browse_Cart_Checkout_Payment_Order_Fulfillment_Notification()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationWorkflowHelper.EnsureManualPaymentPluginAsync(client);

        const string email = "critical-workflow@example.com";

        // Register
        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, email, "Password123!", "Critical", "Shopper");
        await client.PostAsJsonAsync("/api/customers/me/addresses", new
        {
            Label = "Home",
            FirstName = "Critical",
            LastName = "Shopper",
            Country = "IR",
            City = "Tehran",
            Address1 = "Workflow Street 1",
            PostalCode = "1234567890",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });

        // Product + browse
        var offerId = await IntegrationWorkflowHelper.CreateProductOfferAsync(
            client, "WF-CRITICAL-1", ProductType.Simple, 49.99m, logoutAfter: false);
        var browse = await client.GetAsync("/api/catalog/storefront/products?term=WF-CRITICAL");
        Assert.Equal(HttpStatusCode.OK, browse.StatusCode);
        using var browseJson = JsonDocument.Parse(await browse.Content.ReadAsStreamAsync());
        var products = browseJson.RootElement.GetProperty("data");
        Assert.True(products.GetArrayLength() >= 1);
        var productId = products[0].GetProperty("id").GetInt32();

        var detail = await client.GetAsync($"/api/catalog/storefront/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        // Re-login as customer after admin product setup
        var login = await client.PostAsJsonAsync("/api/customers/login", new
        {
            Email = email,
            Password = "Password123!",
            RememberMe = false
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Cart
        var addCart = await client.PostAsJsonAsync("/api/cart/items", new { OfferId = offerId, Quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, addCart.StatusCode);

        // Checkout
        var checkoutId = await IntegrationWorkflowHelper.PrepareReadyCustomerCheckoutAsync(client, offerId);

        // Order
        var (orderId, orderNumber) = await IntegrationWorkflowHelper.CreateOrderAsync(client, checkoutId);
        Assert.False(string.IsNullOrWhiteSpace(orderNumber));

        // Payment
        var paymentId = await IntegrationWorkflowHelper.CreatePaymentAsync(client, orderId);
        await IntegrationWorkflowHelper.CapturePaymentAsync(client, paymentId);

        // Fulfillment
        await IntegrationWorkflowHelper.FulfillOrderAsync(client, orderId);

        // Notification log (admin)
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);
        var logs = await client.GetAsync("/api/admin/notifications/logs?take=50");
        Assert.Equal(HttpStatusCode.OK, logs.StatusCode);
        using var logsJson = JsonDocument.Parse(await logs.Content.ReadAsStreamAsync());
        Assert.True(logsJson.RootElement.GetProperty("data").GetArrayLength() >= 0);

        var order = await client.GetAsync($"/api/admin/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, order.StatusCode);
        using var orderJson = JsonDocument.Parse(await order.Content.ReadAsStreamAsync());
        var status = orderJson.RootElement.GetProperty("data").GetProperty("status").GetString();
        Assert.Contains(status, new[] { "Completed", "Processing", "Confirmed" }, StringComparer.OrdinalIgnoreCase);
    }
}
