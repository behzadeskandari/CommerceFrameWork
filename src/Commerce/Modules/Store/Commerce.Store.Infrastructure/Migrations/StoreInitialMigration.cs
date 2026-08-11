using Commerce.Framework.Data.Migrations;

namespace Commerce.Store.Infrastructure.Migrations;

public sealed class StoreInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Store_Initial";

    public string Description => "Ensures store, language, currency, and settings schema are available.";

    public string Module => "Commerce.Store";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
