using Commerce.Downloads.Infrastructure.DependencyInjection;
using Commerce.Downloads.Infrastructure.Migrations;
using Commerce.Downloads.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Downloads;

public sealed class DownloadsModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.downloads",
        SystemName: "Commerce.Downloads",
        Name: "Downloads",
        Version: new Version(1, 0, 0),
        Description: "Digital product downloads and secure fulfillment.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Media"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Orders"),
            new ModuleDependency("Commerce.Payments")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, DownloadsModelContributor>();
        services.AddSingleton<ICommerceMigration, DownloadsInitialMigration>();
        services.AddDownloadsInfrastructure();
    }
}
