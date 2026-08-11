using System.Net;
using System.Text.Json;
using Commerce.Framework.Contracts.Modules;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class ModuleRuntimeFlowTests
{
    [Fact]
    public async Task AfterInstallation_ModuleRuntimeStartsCoreModule()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var response = await client.GetAsync("/modules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var modules = json.RootElement.GetProperty("modules");
        Assert.True(modules.GetArrayLength() >= 1);

        var coreModule = modules.EnumerateArray()
            .First(x => x.GetProperty("systemName").GetString() == "Commerce.Core");

        Assert.Equal(nameof(ModuleState.Started), coreModule.GetProperty("state").GetString());
    }

    [Fact]
    public async Task CompleteInstallationFlow_StillLocksInstallerAfterModuleRuntimeIntroduced()
    {
        await using var factory = InstallationFlowTests.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await InstallationFlowTests.CompleteInstallationAsync(client, InstallationFlowTests.CreateInMemoryToken());

        var locked = await client.GetAsync("/installation");
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
    }
}
