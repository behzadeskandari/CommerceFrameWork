using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Data.Seeding;

public sealed class InstallationMetadataSeeder : ICommerceSeeder
{
    public int Order => 10;

    public string Name => "InstallationMetadata";

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var dbContext = context.Services.GetRequiredService<CommerceDbContext>();

        if (!await dbContext.CommerceInstallations.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            dbContext.CommerceInstallations.Add(new CommerceInstallation
            {
                InstallationId = Guid.NewGuid(),
                Status = nameof(Contracts.Installation.InstallationStatus.InProgress),
                CurrentStep = (int)Contracts.Installation.InstallationStep.Seed,
                ApplicationVersion = typeof(InstallationMetadataSeeder).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            });
        }
    }
}

public sealed class DefaultSettingsSeeder : ICommerceSeeder
{
    public int Order => 20;

    public string Name => "DefaultSettings";

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var dbContext = context.Services.GetRequiredService<CommerceDbContext>();

        await EnsureSettingAsync(dbContext, "Commerce.Installation.Completed", "false", cancellationToken)
            .ConfigureAwait(false);
        await EnsureSettingAsync(dbContext, "Catalog.ProductsPerPage", "12", cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task EnsureSettingAsync(
        CommerceDbContext dbContext,
        string name,
        string value,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Settings.AnyAsync(x => x.Name == name && x.StoreId == 0, cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        dbContext.Settings.Add(new Setting
        {
            Name = name,
            Value = value,
            StoreId = 0
        });
    }
}
