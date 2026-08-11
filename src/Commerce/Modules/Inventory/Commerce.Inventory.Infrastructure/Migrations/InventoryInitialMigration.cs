using Commerce.Framework.Data.Migrations;

namespace Commerce.Inventory.Infrastructure.Migrations;

public sealed class InventoryInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Inventory_Initial";

    public string Description => "Ensures inventory schema is available.";

    public string Module => "Commerce.Inventory";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
