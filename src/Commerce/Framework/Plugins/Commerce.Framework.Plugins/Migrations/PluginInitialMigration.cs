using Commerce.Framework.Data.Migrations;

namespace Commerce.Framework.Plugins.Migrations;

public sealed class PluginInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Plugins_Initial";

    public string Description => "Ensures plugin installation schema is available.";

    public string Module => "Commerce.Plugins";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
