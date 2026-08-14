using Commerce.Framework.Data.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Plugin.Test.Migrations;

public sealed class TestPluginInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "CommerceTest_Initial";

    public string Description => "Ensures test plugin schema marker is available.";

    public string Module => "Commerce.Test";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
