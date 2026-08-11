using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Catalog.Domain.Enums;
using Commerce.Host.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class CatalogFlowTests
{
    private const string AdminKey = "integration-catalog-admin-key";

    [Fact]
    public async Task CompleteCatalogFlow_WorksAfterInstallation()
    {
        await using var factory = InstallationFlowTests.CreateFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Commerce:Catalog:AdminApiKey"] = AdminKey
                    });
                });
            });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add(CatalogAdminRequiredAttribute.AdminKeyHeader, AdminKey);

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var categoryResponse = await client.PostAsJsonAsync("/api/catalog/categories", new
        {
            Name = "Games",
            Published = true
        });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        using var categoryJson = JsonDocument.Parse(await categoryResponse.Content.ReadAsStreamAsync());
        var categoryId = categoryJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var productResponse = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            Name = "Sample Game",
            Sku = "GAME-001",
            ProductType = ProductType.Simple,
            Published = true,
            CategoryIds = new[] { categoryId }
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStreamAsync());
        var productId = productJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var getProduct = await client.GetAsync($"/api/catalog/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, getProduct.StatusCode);

        var updateProduct = await client.PutAsJsonAsync($"/api/catalog/products/{productId}", new
        {
            Name = "Sample Game Updated",
            ProductType = ProductType.Simple,
            Published = true,
            CategoryIds = new[] { categoryId }
        });
        Assert.Equal(HttpStatusCode.OK, updateProduct.StatusCode);

        var deleteProduct = await client.DeleteAsync($"/api/catalog/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, deleteProduct.StatusCode);

        var deletedRead = await client.GetAsync($"/api/catalog/products/{productId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedRead.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithoutAdminKey_ReturnsUnauthorized()
    {
        await using var factory = InstallationFlowTests.CreateFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Commerce:Catalog:AdminApiKey"] = AdminKey
                    });
                });
            });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var response = await client.PostAsJsonAsync("/api/catalog/products", new
        {
            Name = "Blocked",
            Sku = "BLOCK-1",
            ProductType = ProductType.Simple
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
