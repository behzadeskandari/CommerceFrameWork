using Commerce.Audit.Infrastructure.DependencyInjection;
using Commerce.Framework.Contracts.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Audit;

public sealed class AuditModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.audit",
        SystemName: "Commerce.Audit",
        Name: "Audit",
        Version: new Version(1, 0, 0),
        Description: "Tamper-resistant audit logging and security compliance infrastructure.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Customers")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddAuditInfrastructure(configuration);
}
