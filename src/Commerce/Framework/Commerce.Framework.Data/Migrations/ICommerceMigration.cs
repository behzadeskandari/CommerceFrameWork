namespace Commerce.Framework.Data.Migrations;

public interface ICommerceMigration
{
    string Version { get; }
    string Name { get; }
    string Description { get; }
    string Module { get; }

    Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default);

    Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default);
}
