using Commerce.Framework.Data.Migrations;

namespace Commerce.Catalog.Infrastructure.Migrations;

public sealed class CatalogPhase21Migration : ICommerceMigration
{
    public string Version => "1.1.0";

    public string Name => "Catalog_Phase21_OfferTierPrices";

    public string Description => "Ensures offer tier pricing schema is available.";

    public string Module => "Commerce.Catalog";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
