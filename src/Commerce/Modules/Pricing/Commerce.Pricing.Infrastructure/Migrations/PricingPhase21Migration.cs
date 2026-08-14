using Commerce.Framework.Data.Migrations;

namespace Commerce.Pricing.Infrastructure.Migrations;

public sealed class PricingPhase21Migration : ICommerceMigration
{
    public string Version => "1.1.0";

    public string Name => "Pricing_Phase21_CustomerGroups";

    public string Description => "Ensures customer group pricing schema is available.";

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
