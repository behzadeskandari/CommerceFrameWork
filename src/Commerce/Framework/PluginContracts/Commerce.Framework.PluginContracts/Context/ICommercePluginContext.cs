using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.PluginContracts.Context;

public interface ICommercePluginContext
{
    PluginDescriptor Descriptor { get; }

    IServiceProvider Services { get; }

    IConfiguration Configuration { get; }

    ILogger Logger { get; }
}
