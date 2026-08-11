using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;

namespace Commerce.Catalog.Infrastructure.Migrations;

public sealed class CatalogInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Catalog_Initial";

    public string Description => "Ensures catalog schema is available.";

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
