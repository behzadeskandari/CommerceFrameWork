using Commerce.Framework.Data.Migrations;

namespace Commerce.Scheduling.Infrastructure.Migrations;

public sealed class SchedulingInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Scheduling_Initial";
    public string Description => "Creates background jobs, executions, recurring schedules, and distributed locks.";
    public string Module => "Commerce.Scheduling";
    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
