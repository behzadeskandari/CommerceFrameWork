using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Commerce.Tests.Integration;

[Trait("Category", "E2E")]
[Trait("Category", "Plugin")]
[Trait("Phase", "45")]
public sealed class PluginLifecycleWorkflowTests
{
    private const string PluginSystemName = "Payment.Manual";

    [Fact]
    public async Task Install_Migration_Enable_Settings_Permission_Disable_Uninstall()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        // Discover
        var list = await client.GetAsync("/api/admin/plugins");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        // Install
        var detail = await client.GetAsync($"/api/admin/plugins/{PluginSystemName}");
        detail.EnsureSuccessStatusCode();
        using (var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStreamAsync()))
        {
            if (!detailJson.RootElement.GetProperty("data").GetProperty("isInstalled").GetBoolean())
            {
                var install = await client.PostAsync($"/api/admin/plugins/{PluginSystemName}/install", null);
                install.EnsureSuccessStatusCode();
            }
        }

        // Migration status
        var migrations = await client.GetAsync($"/api/admin/plugins/{PluginSystemName}/migrations");
        Assert.Equal(HttpStatusCode.OK, migrations.StatusCode);

        // Enable
        var enable = await client.PostAsync($"/api/admin/plugins/{PluginSystemName}/enable", null);
        enable.EnsureSuccessStatusCode();

        // Settings
        var settingsGet = await client.GetAsync($"/api/admin/plugins/{PluginSystemName}/settings");
        Assert.Equal(HttpStatusCode.OK, settingsGet.StatusCode);

        var settingsSave = await client.PutAsJsonAsync(
            $"/api/admin/plugins/{PluginSystemName}/settings",
            new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.OK, settingsSave.StatusCode);

        // Permissions
        var permissions = await client.GetAsync($"/api/admin/plugins/{PluginSystemName}/permissions");
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);

        // Controller surface (admin plugin detail acts as controller metadata)
        var ui = await client.GetAsync($"/api/admin/plugins/{PluginSystemName}/ui");
        Assert.True(ui.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);

        // Disable
        var disable = await client.PostAsync($"/api/admin/plugins/{PluginSystemName}/disable", null);
        disable.EnsureSuccessStatusCode();

        using (var disabledJson = JsonDocument.Parse(await (await client.GetAsync($"/api/admin/plugins/{PluginSystemName}")).Content.ReadAsStreamAsync()))
        {
            Assert.False(disabledJson.RootElement.GetProperty("data").GetProperty("isEnabled").GetBoolean());
        }

        // Re-enable before uninstall so payment tests are not broken in shared factory - actually fresh factory per test
        await client.PostAsync($"/api/admin/plugins/{PluginSystemName}/enable", null);

        // Uninstall (keep data)
        var uninstall = await client.PostAsync(
            $"/api/admin/plugins/{PluginSystemName}/uninstall?uninstallMode=KeepData",
            null);
        Assert.Equal(HttpStatusCode.OK, uninstall.StatusCode);

        using var finalJson = JsonDocument.Parse(await (await client.GetAsync($"/api/admin/plugins/{PluginSystemName}")).Content.ReadAsStreamAsync());
        Assert.False(finalJson.RootElement.GetProperty("data").GetProperty("isInstalled").GetBoolean());
    }
}
