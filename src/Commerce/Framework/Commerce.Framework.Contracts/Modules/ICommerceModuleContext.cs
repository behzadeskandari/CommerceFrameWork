using Commerce.Framework.Contracts.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Contracts.Modules;

public interface ICommerceModuleContext
{
    ModuleDescriptor Descriptor { get; }

    IServiceProvider Services { get; }

    IConfiguration Configuration { get; }

    IStoreContext StoreContext { get; }

    ILogger Logger { get; }
}
