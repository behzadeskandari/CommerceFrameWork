using Commerce.Framework.Data.Migrations;

namespace Commerce.Search.Infrastructure.Migrations;

public sealed class SearchInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Search_Initial";
    public string Description => "Ensures search schema is available.";
    public string Module => "Commerce.Search";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
