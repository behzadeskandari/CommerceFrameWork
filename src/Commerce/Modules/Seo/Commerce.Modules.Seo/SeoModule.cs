using Commerce.Seo.Infrastructure.DependencyInjection;
using Commerce.Seo.Infrastructure.Migrations;
using Commerce.Seo.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Seo;

public sealed class SeoModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.seo",
        SystemName: "Commerce.Seo",
        Name: "SEO",
        Version: new Version(1, 0, 0),
        Description: "SEO metadata, friendly URLs, sitemap, and robots.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, SeoModelContributor>();
        services.AddSingleton<ICommerceMigration, SeoInitialMigration>();
        services.AddSeoInfrastructure();
    }
}
