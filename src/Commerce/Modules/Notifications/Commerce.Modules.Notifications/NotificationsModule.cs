using Commerce.Notifications.Infrastructure.DependencyInjection;
using Commerce.Notifications.Infrastructure.Migrations;
using Commerce.Notifications.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Notifications;

public sealed class NotificationsModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.notifications",
        SystemName: "Commerce.Notifications",
        Name: "Notifications",
        Version: new Version(1, 0, 0),
        Description: "Provider-independent customer notifications (email, SMS, in-app).",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Orders"),
            new ModuleDependency("Commerce.Downloads"),
            new ModuleDependency("Commerce.Scheduling")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICommerceModelContributor, NotificationsModelContributor>();
        services.AddSingleton<ICommerceMigration, NotificationsInitialMigration>();
        services.AddNotificationsInfrastructure();
    }
}
