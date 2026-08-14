using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace __ROOT_NAMESPACE__;

public sealed class __NAME__Plugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<__NAME__PluginState>();
    }

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Services.GetRequiredService<__NAME__PluginState>().MarkStarted();
        return Task.CompletedTask;
    }

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public sealed class __NAME__PluginState
{
    public bool HasStarted { get; private set; }

    public void MarkStarted() => HasStarted = true;
}
