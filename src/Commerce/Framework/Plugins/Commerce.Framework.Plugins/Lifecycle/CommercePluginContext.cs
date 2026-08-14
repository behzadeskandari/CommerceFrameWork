using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Plugins.Lifecycle;

public sealed class CommercePluginContext(
    PluginDescriptor descriptor,
    IServiceProvider services,
    IConfiguration configuration,
    ILogger logger) : ICommercePluginContext
{
    public PluginDescriptor Descriptor { get; } = descriptor;

    public IServiceProvider Services { get; } = services;

    public IConfiguration Configuration { get; } = configuration;

    public ILogger Logger { get; } = logger;
}
