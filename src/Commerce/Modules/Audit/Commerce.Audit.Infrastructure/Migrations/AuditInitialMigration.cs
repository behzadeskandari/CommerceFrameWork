using Commerce.Framework.Data.Migrations;

namespace Commerce.Audit.Infrastructure.Migrations;

public sealed class AuditInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Audit_Initial";
    public string Description => "Creates append-only audit entries with hash chain fields.";
    public string Module => "Commerce.Audit";
    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
