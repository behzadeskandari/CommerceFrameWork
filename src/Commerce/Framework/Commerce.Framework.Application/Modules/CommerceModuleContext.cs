using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Application.Modules;

public sealed class CommerceModuleContext : ICommerceModuleContext
{
    public CommerceModuleContext(
        ModuleDescriptor descriptor,
        IServiceProvider services,
        IConfiguration configuration,
        IStoreContext storeContext,
        ILogger logger)
    {
        Descriptor = descriptor;
        Services = services;
        Configuration = configuration;
        StoreContext = storeContext;
        Logger = logger;
    }

    public ModuleDescriptor Descriptor { get; }

    public IServiceProvider Services { get; }

    public IConfiguration Configuration { get; }

    public IStoreContext StoreContext { get; }

    public ILogger Logger { get; }
}
