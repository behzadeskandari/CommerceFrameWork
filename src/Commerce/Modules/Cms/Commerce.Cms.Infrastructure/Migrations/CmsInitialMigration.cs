using Commerce.Framework.Data.Migrations;

namespace Commerce.Cms.Infrastructure.Migrations;

public sealed class CmsInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Cms_Initial";
    public string Description => "Ensures CMS schema is available.";
    public string Module => "Commerce.Cms";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
