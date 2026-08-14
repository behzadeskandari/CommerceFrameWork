using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Integration;

[Trait("Category", "E2E")]
[Trait("Category", "Workflow")]
[Trait("Phase", "45")]
public sealed class DigitalProductWorkflowTests
{
    private static readonly byte[] MinimalZip =
    [
        0x50, 0x4B, 0x03, 0x04, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x50, 0x4B,
        0x01, 0x02, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x50, 0x4B,
        0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    [Fact]
    public async Task DigitalProduct_Cart_Payment_Order_DownloadEntitlement_Download()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationWorkflowHelper.EnsureManualPaymentPluginAsync(client);

        const string email = "digital-workflow@example.com";

        var (offerId, productId) = await IntegrationWorkflowHelper.CreateProductWithOfferAsync(
            client, "WF-DIGITAL-1", ProductType.Digital, 19.99m, logoutAfter: false);

        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var mediaId = await IntegrationWorkflowHelper.UploadMediaAsync(
            client, MinimalZip, "digital-product.zip", "application/zip", isPublic: false);

        await client.PutAsJsonAsync($"/api/admin/downloads/products/{productId}/settings", new
        {
            isEnabled = true,
            maxDownloadCount = 5,
            expirationDays = 30
        });

        var addFile = await client.PostAsJsonAsync($"/api/admin/downloads/products/{productId}/files", new
        {
            mediaAssetId = mediaId,
            displayName = "Digital Asset",
            displayOrder = 0,
            isActive = true
        });
        addFile.EnsureSuccessStatusCode();

        await IntegrationWorkflowHelper.LogoutAsync(client);

        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, email, "Password123!");
        await client.PostAsJsonAsync("/api/customers/me/addresses", new
        {
            Label = "Home",
            FirstName = "Digital",
            LastName = "Buyer",
            Country = "IR",
            City = "Tehran",
            Address1 = "Digital Lane",
            PostalCode = "9999999999",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });

        var checkoutId = await IntegrationWorkflowHelper.PrepareReadyCustomerCheckoutAsync(client, offerId);
        var (orderId, _) = await IntegrationWorkflowHelper.CreateOrderAsync(client, checkoutId);
        var paymentId = await IntegrationWorkflowHelper.CreatePaymentAsync(client, orderId);
        await IntegrationWorkflowHelper.CapturePaymentAsync(client, paymentId);

        var downloads = await client.GetAsync("/api/downloads");
        Assert.Equal(HttpStatusCode.OK, downloads.StatusCode);
        using var downloadsJson = JsonDocument.Parse(await downloads.Content.ReadAsStreamAsync());
        var entitlements = downloadsJson.RootElement.GetProperty("data");
        Assert.True(entitlements.GetArrayLength() >= 1);

        var entitlementId = entitlements[0].GetProperty("id").GetInt32();
        var fileId = entitlements[0].GetProperty("files")[0].GetProperty("id").GetInt32();

        var file = await client.GetAsync($"/api/downloads/{entitlementId}/files/{fileId}");
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.True(file.Content.Headers.ContentLength > 0 || file.Content.Headers.ContentType is not null);
    }
}
