using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Scheduling.Infrastructure.DependencyInjection;
using Commerce.Scheduling.Infrastructure.Migrations;
using Commerce.Scheduling.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Scheduling;

public sealed class SchedulingModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.scheduling",
        SystemName: "Commerce.Scheduling",
        Name: "Scheduling",
        Version: new Version(1, 0, 0),
        Description: "Provider-independent background job infrastructure.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core")
        ],
        IsRequired: true);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, SchedulingModelContributor>();
        services.AddSingleton<ICommerceMigration, SchedulingInitialMigration>();
        services.AddSchedulingInfrastructure();
    }

    public override async Task StartAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default)
    {
        await context.Services.RegisterDefaultRecurringJobsAsync(cancellationToken).ConfigureAwait(false);
    }
}
