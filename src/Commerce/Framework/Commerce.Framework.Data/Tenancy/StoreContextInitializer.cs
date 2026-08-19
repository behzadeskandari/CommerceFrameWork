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
        Console.WriteLine("STORE CONTEXT INITIALIZER START");

        using var scope = _serviceProvider.CreateScope();

        Console.WriteLine("Created scope");

        var installationState =
            scope.ServiceProvider.GetRequiredService<IInstallationStateService>();

        Console.WriteLine("Checking installation state");


        if (!await installationState.IsInstalledAsync(cancellationToken))
        {
            Console.WriteLine("NOT INSTALLED");
            return;
        }

        Console.WriteLine("INSTALLED");


        await scope.ServiceProvider
            .GetRequiredService<IStoreContextInitializerService>()
            .InitializeAsync(cancellationToken);

        Console.WriteLine("STORE INITIALIZED");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
