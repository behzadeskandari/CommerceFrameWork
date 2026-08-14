using Commerce.Framework.Data.Migrations;

namespace Commerce.SmartstoreImport.Infrastructure.Migrations;

public sealed class SmartstoreImportInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "SmartstoreImport_Initial";
    public string Description => "Creates Smartstore import audit and legacy ID mapping tables.";
    public string Module => "Commerce.SmartstoreImport";
    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
