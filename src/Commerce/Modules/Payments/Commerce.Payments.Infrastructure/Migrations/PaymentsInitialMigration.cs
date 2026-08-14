using Commerce.Framework.Data.Migrations;



namespace Commerce.Payments.Infrastructure.Migrations;



public sealed class PaymentsInitialMigration : ICommerceMigration

{

    public string Version => "1.0.0";



    public string Name => "Payments_Initial";



    public string Description => "Ensures payments schema is available.";



    public string Module => "Commerce.Payments";



    public Task UpAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default)

    {

        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;

    }



    public Task DownAsync(MigrationExecutionContext context, CancellationToken cancellationToken = default) =>

        Task.CompletedTask;

}

