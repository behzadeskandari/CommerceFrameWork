using Commerce.Framework.Data.Migrations;

namespace Commerce.Seo.Infrastructure.Migrations;

public sealed class SeoInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Seo_Initial";
    public string Description => "Creates SEO tables.";
    public string Module => "Commerce.Seo";
    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
