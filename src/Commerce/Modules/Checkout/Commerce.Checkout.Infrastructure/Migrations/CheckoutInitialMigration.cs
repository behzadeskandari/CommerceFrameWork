using Commerce.Framework.Data.Migrations;

namespace Commerce.Checkout.Infrastructure.Migrations;

public sealed class CheckoutInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Checkout_Initial";

    public string Description => "Ensures checkout schema is available.";

    public string Module => "Commerce.Checkout";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
