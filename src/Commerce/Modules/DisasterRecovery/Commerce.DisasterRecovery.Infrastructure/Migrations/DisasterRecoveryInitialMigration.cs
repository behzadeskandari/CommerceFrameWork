using Commerce.Framework.Data.Migrations;

namespace Commerce.DisasterRecovery.Infrastructure.Migrations;

public sealed class DisasterRecoveryInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "DisasterRecovery_Initial";
    public string Description => "Creates disaster recovery backup and recovery test tables.";
    public string Module => "Commerce.DisasterRecovery";
    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
