using Commerce.Cms.Infrastructure.DependencyInjection;
using Commerce.Cms.Infrastructure.Migrations;
using Commerce.Cms.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Cms;

public sealed class CmsModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.cms",
        SystemName: "Commerce.Cms",
        Name: "CMS",
        Version: new Version(1, 0, 0),
        Description: "Pages, topics, widgets, and menus.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, CmsModelContributor>();
        services.AddSingleton<ICommerceMigration, CmsInitialMigration>();
        services.AddCmsInfrastructure();
    }
}
