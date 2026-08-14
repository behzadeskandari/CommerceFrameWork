using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Application.DependencyInjection;
using Commerce.Scheduling.Infrastructure.Persistence.Repositories;
using Commerce.Scheduling.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Commerce.Scheduling.Infrastructure.Health;
using Commerce.Framework.Contracts.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Commerce.Scheduling.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, SchedulingPermissionContributor>();
        services.Replace(ServiceDescriptor.Singleton<ISchedulingHealthProbe, SchedulingHealthProbe>());
        services.AddScoped<ISchedulingRepository, EfSchedulingRepository>();
        services.AddSchedulingApplication();
        return services;
    }
}

public static class SchedulingStartupExtensions
{
    public static async Task RegisterDefaultRecurringJobsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

        await scheduler.RegisterRecurringAsync(
            new RegisterRecurringJobRequest("notifications.retry", BackgroundJobTypes.NotificationRetry, 60),
            cancellationToken).ConfigureAwait(false);

        await scheduler.RegisterRecurringAsync(
            new RegisterRecurringJobRequest("search.index.process", BackgroundJobTypes.SearchIndexProcess, 30),
            cancellationToken).ConfigureAwait(false);

        await scheduler.RegisterRecurringAsync(
            new RegisterRecurringJobRequest("maintenance.cleanup", BackgroundJobTypes.MaintenanceCleanup, 86400),
            cancellationToken).ConfigureAwait(false);
    }
}
