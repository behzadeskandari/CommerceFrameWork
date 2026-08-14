using Commerce.Reviews.Infrastructure.DependencyInjection;
using Commerce.Reviews.Infrastructure.Migrations;
using Commerce.Reviews.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Reviews;

public sealed class ReviewsModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.reviews",
        SystemName: "Commerce.Reviews",
        Name: "Reviews",
        Version: new Version(1, 0, 0),
        Description: "Product reviews, ratings, and customer wishlists.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Catalog"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Orders")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, ReviewsModelContributor>();
        services.AddSingleton<ICommerceMigration, ReviewsInitialMigration>();
        services.AddReviewsInfrastructure();
    }
}
