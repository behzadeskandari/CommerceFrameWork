using Commerce.Framework.Data.Migrations;

namespace Commerce.Downloads.Infrastructure.Migrations;

public sealed class DownloadsInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Downloads_Initial";

    public string Description => "Creates digital product download tables.";

    public string Module => "Commerce.Downloads";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
