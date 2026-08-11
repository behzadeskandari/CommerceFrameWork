using Commerce.Framework.Data.Migrations;

namespace Commerce.Customers.Infrastructure.Migrations;

public sealed class CustomersInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Customers_Initial";

    public string Description => "Ensures customers schema and identity integration are available.";

    public string Module => "Commerce.Customers";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
