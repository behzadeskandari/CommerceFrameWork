using Commerce.Observability.Application.DependencyInjection;
using Commerce.Observability.Infrastructure.Correlation;
using Commerce.Observability.Infrastructure.HealthChecks;
using Commerce.Observability.Infrastructure.Middleware;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Infrastructure.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Commerce.Observability.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObservabilityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ISchedulingHealthProbe, NullSchedulingHealthProbe>();
        services.TryAddSingleton<IPaymentProviderHealthProbe, NullPaymentProviderHealthProbe>();
        services.Replace(ServiceDescriptor.Singleton<ICacheHealthProbe, DefaultCacheHealthProbe>());
        services.Replace(ServiceDescriptor.Singleton<IPaymentProviderHealthProbe, PaymentProviderHealthProbe>());
        services.TryAddSingleton<IBackupHealthProbe, NullBackupHealthProbe>();
        services.AddScoped<JobCorrelationContext>();
        services.Replace(ServiceDescriptor.Scoped<ICorrelationContext, HttpCorrelationContext>());
        services.AddObservabilityApplication();

        services.AddHealthChecks()
            .AddCheck<LivenessHealthCheck>("liveness", tags: ["live"])
            .AddCheck<ReadinessHealthCheck>("readiness", tags: ["ready"])
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready", "dependency"])
            .AddCheck<CacheHealthCheck>("cache", tags: ["ready", "dependency"])
            .AddCheck<SchedulingHealthCheck>("scheduling", tags: ["ready", "dependency"])
            .AddCheck<PluginHealthCheck>("plugins", tags: ["ready", "dependency"])
            .AddCheck<ModuleHealthCheck>("modules", tags: ["ready", "dependency"])
            .AddCheck<PaymentProviderHealthCheck>("payment_providers", tags: ["ready", "dependency"])
            .AddCheck<BackupHealthCheck>("backups", tags: ["ready", "dependency"]);

        return services;
    }
}

public static class ObservabilityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCommerceCorrelation(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    public static IApplicationBuilder UseCommerceRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();

    public static WebApplication MapCommerceHealthChecks(this WebApplication app)
    {
        static Task WriteHealthResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.ToDictionary(
                    pair => pair.Key,
                    pair => new
                    {
                        status = pair.Value.Status.ToString(),
                        description = pair.Value.Description,
                        durationMs = pair.Value.Duration.TotalMilliseconds,
                        data = pair.Value.Data
                    })
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("dependency"),
            ResponseWriter = WriteHealthResponse
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponse
        });

        return app;
    }
}
