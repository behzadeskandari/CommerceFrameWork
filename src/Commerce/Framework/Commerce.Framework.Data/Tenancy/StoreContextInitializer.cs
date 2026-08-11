using Commerce.Framework.Contracts.Installation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Data.Tenancy;

public sealed class StoreContextInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StoreContextInitializer> _logger;

    public StoreContextInitializer(IServiceProvider serviceProvider, ILogger<StoreContextInitializer> logger)
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
            return;
        }

        await scope.ServiceProvider
            .GetRequiredService<IStoreContextInitializerService>()
            .InitializeAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
