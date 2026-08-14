using Commerce.Framework.Data.Migrations;

namespace Commerce.Shipping.Infrastructure.Migrations;

public sealed class ShippingInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Shipping_Initial";

    public string Description => "Ensures shipping schema is available.";

    public string Module => "Commerce.Shipping";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
