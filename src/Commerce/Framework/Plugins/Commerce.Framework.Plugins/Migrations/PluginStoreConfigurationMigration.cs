using Commerce.Framework.Data.Migrations;

namespace Commerce.Framework.Plugins.Migrations;

public sealed class PluginStoreConfigurationMigration : ICommerceMigration
{
    public string Version => "1.1.0";

    public string Name => "Plugins_StoreConfiguration";

    public string Description => "Adds plugin store-scoped configuration table.";

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
