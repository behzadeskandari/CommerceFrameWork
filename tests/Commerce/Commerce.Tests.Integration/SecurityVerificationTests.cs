using System.Net;
using Xunit;

namespace Commerce.Tests.Integration;

[Trait("Category", "Security")]
[Trait("Phase", "45")]
public sealed class SecurityVerificationTests
{
    [Fact]
    public async Task AdminOrders_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var response = await client.GetAsync("/api/admin/orders");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401/403 but got {response.StatusCode}");
    }

    [Fact]
    public async Task CustomerDownloads_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var response = await client.GetAsync("/api/downloads");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Installation_Endpoints_LockedAfterComplete()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var locked = await client.GetAsync("/installation");
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
    }

    [Fact]
    public async Task PrivateMedia_AnonymousAccess_Denied()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var adminClient = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(adminClient, InstallationFlowTests.CreateInMemoryToken());

        var mediaId = await IntegrationWorkflowHelper.UploadMediaAsync(
            adminClient,
            MediaFlowTestsMinimalPng.Bytes,
            "secure.png",
            "image/png",
            isPublic: false);

        using var anon = factory.CreateClient();
        var response = await anon.GetAsync($"/api/media/{mediaId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

internal static class MediaFlowTestsMinimalPng
{
    internal static readonly byte[] Bytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];
}
