using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class MediaFlowTests
{
    private static readonly byte[] MinimalPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];

    [Fact]
    public async Task UploadPublicMedia_CanDownload_AndAssignToProduct()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(MinimalPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test-image.png");
        content.Add(new StringContent("true"), "isPublic");

        var upload = await client.PostAsync("/api/media/upload", content);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploadJson = JsonDocument.Parse(await upload.Content.ReadAsStreamAsync());
        var mediaId = uploadJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var publicDownload = await client.GetAsync($"/api/media/{mediaId}");
        Assert.Equal(HttpStatusCode.OK, publicDownload.StatusCode);

        var productResponse = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            Name = "Media Product",
            Sku = "MEDIA-PROD-1",
            ProductType = ProductType.Simple,
            Published = true,
            IsVisible = true,
            IsAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var assign = await client.PostAsJsonAsync($"/api/catalog/products/{productId}/media", new
        {
            MediaAssetId = mediaId,
            Role = "Primary",
            DisplayOrder = 0
        });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
    }

    [Fact]
    public async Task PrivateMedia_NotAccessibleAnonymously()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(MinimalPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "private.png");
        content.Add(new StringContent("false"), "isPublic");

        var upload = await client.PostAsync("/api/media/upload", content);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploadJson = JsonDocument.Parse(await upload.Content.ReadAsStreamAsync());
        var mediaId = uploadJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        using var anonClient = factory.CreateClient();
        var anonymous = await anonClient.GetAsync($"/api/media/{mediaId}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}
