using Commerce.Search.Infrastructure.DependencyInjection;
using Commerce.Search.Infrastructure.Migrations;
using Commerce.Search.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Search;

public sealed class SearchModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.search",
        SystemName: "Commerce.Search",
        Name: "Search",
        Version: new Version(1, 0, 0),
        Description: "Provider-independent product search and indexing.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Catalog")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCommerceSearch();
        services.AddSingleton<ICommerceModelContributor, SearchModelContributor>();
        services.AddSingleton<ICommerceMigration, SearchInitialMigration>();
        services.AddSearchInfrastructure();
    }
}
