using System.Net;
using System.Net.Http.Json;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Data.Installation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Commerce.Tests.Integration;

public sealed class InstallationFlowTests
{
    [Fact]
    public async Task CompleteInstallationFlow_LocksInstallerAfterFinish()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await CompleteInstallationAsync(client, CreateInMemoryToken());

        var locked = await client.GetAsync("/installation");
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
    }

    internal static WebApplicationFactory<Program> CreateFactory()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(WebHostDefaults.ContentRootKey, contentRoot);
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Commerce:ApplicationName"] = "Commerce",
                        ["Commerce:Environment"] = "Development",
                        ["Commerce:BaseUrl"] = "https://localhost:5100"
                    });
                });
            });
    }

    internal static string CreateInMemoryToken() =>
        $"{DynamicCommerceDbContextConfigurator.InMemoryConnectionToken}:{Guid.NewGuid():N}";

    internal static async Task CompleteInstallationAsync(HttpClient client, string inMemoryToken)
    {
        var requirements = await client.PostAsync("/installation/requirements", null);
        Assert.Equal(HttpStatusCode.OK, requirements.StatusCode);

        var database = await client.PostAsJsonAsync("/installation/database", new DatabaseSetupRequest(
            "SqlServer",
            inMemoryToken));
        Assert.Equal(HttpStatusCode.OK, database.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/installation/migrate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/installation/seed", null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/installation/admin", new AdministratorSetupRequest(
            "admin@example.com",
            "admin",
            "Password123!"))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/installation/store", new StoreSetupRequest(
            "Default Store",
            "https://localhost:5100",
            "localhost"))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/installation/language", new LanguageSetupRequest(
            "English",
            "en-US",
            Rtl: false,
            IsDefault: true))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/installation/currency", new CurrencySetupRequest(
            "US Dollar",
            "USD",
            Rate: 1m,
            IsPrimary: true))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/installation/complete", null)).StatusCode);
    }
}
