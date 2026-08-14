using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Test;

public sealed class TestPlugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TestPluginState>();
    }

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settingService = context.Services.GetService<ISettingService>();
        if (settingService is null)
        {
            return;
        }

        var simulateFailure = await settingService
            .GetAsync<bool>("Commerce.Test.SimulateFailure", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (simulateFailure == true)
        {
            throw new InvalidOperationException("Commerce.Test failure simulation is enabled.");
        }

        var state = context.Services.GetRequiredService<TestPluginState>();
        state.MarkStarted();
    }

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public sealed class TestPluginState
{
    public bool HasStarted { get; private set; }

    public void MarkStarted() => HasStarted = true;
}
