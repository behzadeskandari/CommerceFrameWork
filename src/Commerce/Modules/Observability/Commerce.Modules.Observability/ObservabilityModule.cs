using Commerce.Observability.Infrastructure.DependencyInjection;
using Commerce.Framework.Contracts.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Observability;

public sealed class ObservabilityModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.observability",
        SystemName: "Commerce.Observability",
        Name: "Observability",
        Version: new Version(1, 0, 0),
        Description: "Structured logging, correlation IDs, metrics, tracing, and health checks.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddObservabilityInfrastructure(configuration);
}
