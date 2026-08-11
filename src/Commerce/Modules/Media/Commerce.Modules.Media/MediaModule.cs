using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Media.Infrastructure.DependencyInjection;
using Commerce.Media.Infrastructure.Migrations;
using Commerce.Media.Infrastructure.Persistence;
using Commerce.Media.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Media;

public sealed class MediaModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.media",
        SystemName: "Commerce.Media",
        Name: "Media",
        Version: new Version(1, 0, 0),
        Description: "Media and file storage module.",
        Dependencies: [new ModuleDependency("Commerce.Core")],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, MediaModelContributor>();
        services.AddSingleton<ICommerceMigration, MediaInitialMigration>();
        services.AddSingleton<IModulePermissionContributor, MediaPermissionContributor>();
        services.AddMediaInfrastructure(configuration);
    }
}
