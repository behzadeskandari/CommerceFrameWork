using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class CustomerFlowTests
{
    [Fact]
    public async Task RegisterLoginAndReadProfile_WorksAfterInstallation()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var email = $"customer-{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(client, email, "Password123!", "Jane", "Shopper");

        var profile = await client.GetAsync("/api/customers/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        using var json = JsonDocument.Parse(await profile.Content.ReadAsStreamAsync());
        Assert.Equal("Jane", json.RootElement.GetProperty("data").GetProperty("firstName").GetString());
        Assert.Equal(email, json.RootElement.GetProperty("data").GetProperty("email").GetString());
    }

    [Fact]
    public async Task CustomerAddress_OwnershipEnforced()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        await InstallationFlowTests.CompleteInstallationAsync(
            factory.CreateClient(),
            InstallationFlowTests.CreateInMemoryToken());

        using var clientA = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        using var clientB = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(clientA, "owner@example.com", "Password123!");
        await IntegrationAuthHelper.RegisterAndLoginCustomerAsync(clientB, "other@example.com", "Password123!");

        var createAddress = await clientA.PostAsJsonAsync("/api/customers/me/addresses", new
        {
            Label = "Home",
            FirstName = "Jane",
            LastName = "Shopper",
            Country = "US",
            City = "Seattle",
            Address1 = "123 Main St",
            PostalCode = "98101",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });
        Assert.Equal(HttpStatusCode.Created, createAddress.StatusCode);

        using var addressJson = JsonDocument.Parse(await createAddress.Content.ReadAsStreamAsync());
        var addressId = addressJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var foreignRead = await clientB.GetAsync($"/api/customers/me/addresses/{addressId}");
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);

        var foreignDelete = await clientB.DeleteAsync($"/api/customers/me/addresses/{addressId}");
        Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient();

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var response = await client.GetAsync("/api/customers/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
