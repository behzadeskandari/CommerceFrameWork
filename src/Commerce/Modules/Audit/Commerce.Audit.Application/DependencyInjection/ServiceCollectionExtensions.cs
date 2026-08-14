using Commerce.Audit.Application.Query;
using Commerce.Audit.Application.Writing;
using Commerce.Audit.Contracts;
using Commerce.Framework.Contracts.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Audit.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        return services;
    }
}
