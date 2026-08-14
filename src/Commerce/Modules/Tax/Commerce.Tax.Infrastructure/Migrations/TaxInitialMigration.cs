using Commerce.Framework.Data.Migrations;



namespace Commerce.Tax.Infrastructure.Migrations;



public sealed class TaxInitialMigration : ICommerceMigration

{

    public string Version => "1.0.0";



    public string Name => "Tax_Initial";



    public string Description => "Ensures tax schema is available.";



    public string Module => "Commerce.Tax";



    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)

    {

        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;

    }



    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>

        Task.CompletedTask;

}


