using Commerce.Framework.Data.Migrations;

namespace Commerce.Orders.Infrastructure.Migrations;

public sealed class OrdersInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Orders_Initial";

    public string Description => "Ensures order schema is available.";

    public string Module => "Commerce.Orders";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
