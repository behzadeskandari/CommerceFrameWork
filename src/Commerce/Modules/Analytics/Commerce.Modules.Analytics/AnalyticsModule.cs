using Commerce.Analytics.Infrastructure.DependencyInjection;
using Commerce.Framework.Contracts.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Analytics;

public sealed class AnalyticsModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.analytics",
        SystemName: "Commerce.Analytics",
        Name: "Analytics",
        Version: new Version(1, 0, 0),
        Description: "Admin dashboard and reporting analytics.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Orders"),
            new ModuleDependency("Commerce.Payments"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Inventory"),
            new ModuleDependency("Commerce.Pricing"),
            new ModuleDependency("Commerce.Promotions"),
            new ModuleDependency("Commerce.Downloads"),
            new ModuleDependency("Commerce.Cart"),
            new ModuleDependency("Commerce.Checkout")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddAnalyticsInfrastructure();
}
