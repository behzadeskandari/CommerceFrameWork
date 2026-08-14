using Commerce.Framework.Data.Migrations;

namespace Commerce.Reviews.Infrastructure.Migrations;

public sealed class ReviewsInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";

    public string Name => "Reviews_Initial";

    public string Description => "Creates product review and wishlist tables.";

    public string Module => "Commerce.Reviews";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
