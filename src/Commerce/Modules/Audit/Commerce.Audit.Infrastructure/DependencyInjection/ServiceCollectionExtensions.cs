using Commerce.Audit.Infrastructure.Migrations;
using Commerce.Audit.Infrastructure.Middleware;
using Commerce.Audit.Application.Abstractions;
using Commerce.Audit.Application.DependencyInjection;
using Commerce.Audit.Application.Query;
using Commerce.Audit.Application.Writing;
using Commerce.Audit.Infrastructure.Persistence;
using Commerce.Audit.Infrastructure.Persistence.Repositories;
using Commerce.Audit.Infrastructure.Security;
using Commerce.Audit.Infrastructure.Writing;
using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Commerce.Audit.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuditRetentionOptions>(configuration.GetSection(AuditRetentionOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddSingleton<IModulePermissionContributor, AuditPermissionContributor>();
        services.AddSingleton<ICommerceModelContributor, AuditModelContributor>();
        services.AddSingleton<ICommerceMigration, AuditInitialMigration>();
        services.AddScoped<IAuditRepository, EfAuditRepository>();
        services.AddScoped<AuditWriter>();
        services.AddScoped<IAuditActorContext, HttpAuditActorContext>();
        services.Replace(ServiceDescriptor.Scoped<IAuditPublisher, AuditingAuditPublisher>());
        services.AddAuditApplication();
        return services;
    }
}

public static class AuditApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCommerceSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();

    public static IApplicationBuilder UseCommerceAdminAudit(this IApplicationBuilder app) =>
        app.UseMiddleware<AdminAuditMiddleware>();
}
