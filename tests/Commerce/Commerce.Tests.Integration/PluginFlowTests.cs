using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Commerce.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class PluginFlowTests
{
    [Fact]
    public async Task ManualPlugin_DiscoverInstallEnable_Works()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var list = await client.GetAsync("/api/admin/plugins");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStreamAsync());
        var plugins = listJson.RootElement.GetProperty("data");
        Assert.True(plugins.GetArrayLength() >= 1);

        var manual = plugins.EnumerateArray()
            .First(x => x.GetProperty("systemName").GetString() == "Payment.Manual");

        if (!manual.GetProperty("isInstalled").GetBoolean())
        {
            var install = await client.PostAsync("/api/admin/plugins/Payment.Manual/install", null);
            Assert.Equal(HttpStatusCode.OK, install.StatusCode);
        }

        var detailBeforeEnable = await client.GetAsync("/api/admin/plugins/Payment.Manual");
        using var detailBeforeEnableJson = JsonDocument.Parse(await detailBeforeEnable.Content.ReadAsStreamAsync());
        var enabledAlready = detailBeforeEnableJson.RootElement.GetProperty("data").GetProperty("isEnabled").GetBoolean();

        if (!enabledAlready)
        {
            var enable = await client.PostAsync("/api/admin/plugins/Payment.Manual/enable", null);
            Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        }

        var detail = await client.GetAsync("/api/admin/plugins/Payment.Manual");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStreamAsync());
        var plugin = detailJson.RootElement.GetProperty("data");
        Assert.True(plugin.GetProperty("isInstalled").GetBoolean());
        Assert.True(plugin.GetProperty("isEnabled").GetBoolean());
        Assert.Equal("Payment.Manual", plugin.GetProperty("systemName").GetString());
    }
}
