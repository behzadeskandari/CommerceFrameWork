using Commerce.Framework.Data.Migrations;

namespace Commerce.Pricing.Infrastructure.Migrations;

public sealed class PricingInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Pricing_Initial";

    public string Description => "Ensures pricing and discount schema is available.";

    public string Module => "Commerce.Pricing";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
