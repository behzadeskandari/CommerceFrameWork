using Commerce.Framework.Data.Db;

namespace Commerce.Framework.Data.Migrations.Core;

public sealed class CoreInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Core_Initial";

    public string Description => "Ensures the commerce migration history schema is available.";

    public string Module => "Core";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        // Phase 1 baseline: MigrationVersionInfo is mapped in CommerceDbContext and created by EnsureCreated.
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
