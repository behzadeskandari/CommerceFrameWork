using Commerce.Framework.Contracts.Modules;

using Commerce.Framework.Contracts.Seeding;

using Commerce.Framework.Data.Db;

using Commerce.Framework.Data.Migrations;

using Commerce.Tax.Infrastructure.DependencyInjection;

using Commerce.Tax.Infrastructure.Migrations;

using Commerce.Tax.Infrastructure.Persistence;

using Commerce.Tax.Infrastructure.Seeding;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;



namespace Commerce.Modules.Tax;



public sealed class TaxModule : CommerceModuleBase

{

    public override ModuleDescriptor Descriptor { get; } = new(

        Id: "commerce.tax",

        SystemName: "Commerce.Tax",

        Name: "Tax",

        Version: new Version(1, 0, 0),

        Description: "Tax categories, zones, rates, and calculation engine.",

        Dependencies:

        [

            new ModuleDependency("Commerce.Core"),

            new ModuleDependency("Commerce.Store"),

            new ModuleDependency("Commerce.Catalog"),

            new ModuleDependency("Commerce.Customers"),

            new ModuleDependency("Commerce.Pricing"),

            new ModuleDependency("Commerce.Checkout")

        ],

        IsRequired: false);



    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)

    {

        services.AddSingleton<ICommerceModelContributor, TaxModelContributor>();

        services.AddSingleton<ICommerceMigration, TaxInitialMigration>();

        services.AddSingleton<ICommerceSeeder, TaxDevelopmentSeeder>();

        services.AddTaxInfrastructure();

    }

}


