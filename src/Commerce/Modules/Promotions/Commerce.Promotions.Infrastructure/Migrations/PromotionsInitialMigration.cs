using Commerce.Framework.Data.Migrations;

namespace Commerce.Promotions.Infrastructure.Migrations;

public sealed class PromotionsInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Promotions_Initial";

    public string Description => "Creates promotion rule engine tables.";

    public string Module => "Commerce.Promotions";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
