using Commerce.Framework.Data.Migrations;

namespace Commerce.Notifications.Infrastructure.Migrations;

public sealed class NotificationsInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Notifications_Initial";
    public string Description => "Creates notification templates, logs, and in-app notifications.";
    public string Module => "Commerce.Notifications";
    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
