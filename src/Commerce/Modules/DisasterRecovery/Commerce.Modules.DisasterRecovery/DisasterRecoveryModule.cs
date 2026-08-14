using Commerce.DisasterRecovery.Infrastructure.DependencyInjection;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.DisasterRecovery;

public sealed class DisasterRecoveryModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.disasterrecovery",
        SystemName: "Commerce.DisasterRecovery",
        Name: "Disaster Recovery",
        Version: new Version(1, 0, 0),
        Description: "Backup, restore verification, and disaster recovery operations.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Scheduling")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddDisasterRecoveryInfrastructure(configuration);

    public override async Task StartAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default) =>
        await context.Services.RegisterDisasterRecoveryRecurringJobsAsync(cancellationToken).ConfigureAwait(false);
}
