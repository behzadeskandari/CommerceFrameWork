using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class AuthSessionTests
{
    [Fact]
    public async Task GetSession_WithoutAuthentication_ReturnsAnonymousSession()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var response = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.False(json.RootElement.GetProperty("data").GetProperty("isAuthenticated").GetBoolean());
    }

    [Fact]
    public async Task GetSession_AfterAdministratorLogin_ReturnsPermissions()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var response = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.True(data.GetProperty("isAuthenticated").GetBoolean());
        Assert.Contains(
            "Catalog.Products.Create",
            data.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()));
    }
}
