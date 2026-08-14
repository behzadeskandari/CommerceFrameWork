using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Search.Database;

public sealed class DatabaseSearchPlugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddDatabaseSearchProvider();

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
