using Commerce.Framework.Contracts.Modules;
using Commerce.SmartstoreImport.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.SmartstoreImport;

public sealed class SmartstoreImportModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.smartstoreimport",
        SystemName: "Commerce.SmartstoreImport",
        Name: "Smartstore Import",
        Version: new Version(1, 0, 0),
        Description: "Repeatable Smartstore SQL export import and legacy ID mapping.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Orders")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddSmartstoreImportInfrastructure(configuration);
}
