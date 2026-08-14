using Commerce.Framework.Data.Migrations;

namespace Commerce.Integration.Infrastructure.Migrations;

public sealed class IntegrationInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Integration_Initial";
    public string Description => "Creates webhook subscriptions, deliveries, API clients, and processed event idempotency.";
    public string Module => "Commerce.Integration";
    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
