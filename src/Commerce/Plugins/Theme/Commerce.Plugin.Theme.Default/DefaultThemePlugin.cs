using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Theme.Default;

public sealed class DefaultThemePlugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddDefaultTheme();

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
