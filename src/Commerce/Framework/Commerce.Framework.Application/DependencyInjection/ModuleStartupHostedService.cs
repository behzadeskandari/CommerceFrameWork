using Commerce.Framework.Application.Modules;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Application.DependencyInjection;

public sealed class ModuleStartupHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleStartupHostedService> _logger;

    public ModuleStartupHostedService(
        IServiceProvider serviceProvider,
        ILogger<ModuleStartupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var installationState = scope.ServiceProvider.GetRequiredService<IInstallationStateService>();

        if (!await installationState.IsInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Commerce is not installed. Module runtime startup skipped.");
            return;
        }

        var manager = scope.ServiceProvider.GetRequiredService<ICommerceModuleManager>();
        _logger.LogInformation("Starting Commerce module runtime.");
        manager.RegisterModules();
        await manager.InitializeModulesAsync(cancellationToken).ConfigureAwait(false);
        await manager.StartModulesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<ICommerceModuleManager>();
        return manager.StopModulesAsync(cancellationToken);
    }
}
