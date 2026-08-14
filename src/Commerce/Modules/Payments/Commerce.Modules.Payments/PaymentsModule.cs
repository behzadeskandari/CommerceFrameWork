using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Payments.Infrastructure.DependencyInjection;
using Commerce.Payments.Infrastructure.Migrations;
using Commerce.Payments.Infrastructure.Persistence;
using Commerce.Payments.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Payments;

public sealed class PaymentsModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.payments",
        SystemName: "Commerce.Payments",
        Name: "Payments",
        Version: new Version(1, 0, 0),
        Description: "Payment processing, methods, and provider integration.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Checkout"),
            new ModuleDependency("Commerce.Orders")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, PaymentsModelContributor>();
        services.AddSingleton<ICommerceMigration, PaymentsInitialMigration>();
        services.AddSingleton<ICommerceSeeder, PaymentsDevelopmentSeeder>();
        services.AddPaymentsInfrastructure();
    }
}
