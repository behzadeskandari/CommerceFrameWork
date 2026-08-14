using Commerce.Cache.Infrastructure.DependencyInjection;
using Commerce.Framework.Contracts.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Cache;

public sealed class CacheModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.cache",
        SystemName: "Commerce.Cache",
        Name: "Caching",
        Version: new Version(1, 0, 0),
        Description: "Distributed caching, output cache, invalidation, and performance optimizations.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Search"),
            new ModuleDependency("Commerce.Store")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddCacheInfrastructure(configuration);
}
