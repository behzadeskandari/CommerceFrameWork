using Commerce.Framework.Data.Migrations;

namespace Commerce.Cart.Infrastructure.Migrations;

public sealed class CartInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Cart_Initial";

    public string Description => "Ensures cart schema is available.";

    public string Module => "Commerce.Cart";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
