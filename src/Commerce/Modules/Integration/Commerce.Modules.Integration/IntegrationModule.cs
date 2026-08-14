using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Events.DependencyInjection;
using Commerce.Framework.Scheduling;
using Commerce.Integration.Infrastructure.DependencyInjection;
using Commerce.Integration.Infrastructure.Migrations;
using Commerce.Integration.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Integration;

public sealed class IntegrationModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.integration",
        SystemName: "Commerce.Integration",
        Name: "Integration",
        Version: new Version(1, 0, 0),
        Description: "Domain events, webhooks, and external API platform.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Orders"),
            new ModuleDependency("Commerce.Customers"),
            new ModuleDependency("Commerce.Scheduling")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCommerceEvents();
        services.AddSingleton<ICommerceModelContributor, IntegrationModelContributor>();
        services.AddSingleton<ICommerceMigration, IntegrationInitialMigration>();
        services.AddIntegrationInfrastructure();
    }

    public override async Task StartAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default)
    {
        using var scope = context.Services.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();
        await scheduler.RegisterRecurringAsync(
            new RegisterRecurringJobRequest("integration.webhooks.deliver", BackgroundJobTypes.WebhookDeliveryProcess, 30),
            cancellationToken).ConfigureAwait(false);
    }
}
