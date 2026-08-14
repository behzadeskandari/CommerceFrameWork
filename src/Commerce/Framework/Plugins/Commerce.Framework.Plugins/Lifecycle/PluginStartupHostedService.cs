using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.PluginContracts.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Plugins.Lifecycle;

public sealed class PluginStartupHostedService(
    IServiceProvider serviceProvider,
    ILogger<PluginStartupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var installationState = scope.ServiceProvider.GetRequiredService<IInstallationStateService>();

        if (!await installationState.IsInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Commerce is not installed. Plugin runtime startup skipped.");
            return;
        }

        var manager = scope.ServiceProvider.GetRequiredService<ICommercePluginManager>();
        logger.LogInformation("Starting Commerce plugin runtime.");

        if (manager is CommercePluginManager concreteManager)
        {
            await concreteManager.SyncInstallationStateAsync(cancellationToken).ConfigureAwait(false);
        }

        manager.Load();
        manager.Register();
        await manager.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await manager.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<ICommercePluginManager>();
            return manager.StopAsync(cancellationToken);
        }
        catch (ObjectDisposedException)
        {
            return Task.CompletedTask;
        }
    }
}
