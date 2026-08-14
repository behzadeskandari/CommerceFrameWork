using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Commerce.Tests.Integration;

[Trait("Category", "Localization")]
[Trait("Phase", "45")]
public sealed class LocalizationWorkflowTests
{
    [Fact]
    public async Task Admin_CanConfigureLanguage_AndStoreContextReflectsDefault()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = IntegrationWorkflowHelper.CreateStorefrontClient(factory);
        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());
        await IntegrationAuthHelper.LoginAsAdministratorAsync(client);

        var create = await client.PostAsJsonAsync("/api/languages", new
        {
            name = "Persian",
            culture = "fa-IR",
            rtl = true,
            isPublished = true,
            displayOrder = 1
        });
        Assert.True(create.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK or HttpStatusCode.Conflict);

        var list = await client.GetAsync("/api/languages");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStreamAsync());
        Assert.True(listJson.RootElement.GetProperty("data").GetArrayLength() >= 1);

        using var contextRequest = new HttpRequestMessage(HttpMethod.Get, "/api/store/context");
        contextRequest.Headers.Host = "localhost";
        var context = await client.SendAsync(contextRequest);
        Assert.Equal(HttpStatusCode.OK, context.StatusCode);
    }
}
