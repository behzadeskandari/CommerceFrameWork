using Commerce.Framework.Data.Migrations;

namespace Commerce.Media.Infrastructure.Migrations;

public sealed class MediaInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Media_Initial";

    public string Description => "Ensures media schema is available.";

    public string Module => "Commerce.Media";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
