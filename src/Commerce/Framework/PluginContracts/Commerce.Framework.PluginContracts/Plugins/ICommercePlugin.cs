using Commerce.Framework.PluginContracts.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.PluginContracts.Plugins;

public interface ICommercePlugin
{
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default);

    Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default);

    Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default);
}
