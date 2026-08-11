using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Contracts.Modules;

public interface ICommerceModule
{
    ModuleDescriptor Descriptor { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    Task InitializeAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default);

    Task StartAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default);
}
