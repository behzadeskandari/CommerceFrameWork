using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Contracts.Modules;

public abstract class CommerceModuleBase : ICommerceModule
{
    public abstract ModuleDescriptor Descriptor { get; }

    public virtual void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public virtual Task InitializeAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public virtual Task StartAsync(ICommerceModuleContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
