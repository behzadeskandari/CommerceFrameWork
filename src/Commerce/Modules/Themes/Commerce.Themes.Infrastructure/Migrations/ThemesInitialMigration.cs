using Commerce.Framework.Data.Migrations;

namespace Commerce.Themes.Infrastructure.Migrations;

public sealed class ThemesInitialMigration : ICommerceMigration
{
    public string Version => "1.0.0";
    public string Name => "Themes_Initial";
    public string Description => "Ensures theme schema is available.";
    public string Module => "Commerce.Themes";

    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.CompletedTask;
    }

    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
