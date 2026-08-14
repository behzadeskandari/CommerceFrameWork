using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Application.Admin;
using Commerce.Scheduling.Application.Execution;
using Commerce.Scheduling.Application.Handlers;
using Commerce.Scheduling.Application.Processing;
using Commerce.Scheduling.Application.Scheduling;
using Commerce.Scheduling.Contracts.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Scheduling.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingApplication(this IServiceCollection services)
    {
        services.AddOptions<BackgroundJobProcessorOptions>();
        services.AddSingleton<BackgroundJobProcessorState>();
        services.AddScoped<IBackgroundJobScheduler, BackgroundJobScheduler>();
        services.AddScoped<BackgroundJobExecutor>();
        services.AddScoped<IBackgroundJobAdminService, BackgroundJobAdminService>();
        services.AddScoped<IJobLockProvider, DatabaseJobLockProvider>();

        RegisterHandler<EmailSendJobHandler>(services);
        RegisterHandler<SmsSendJobHandler>(services);
        RegisterHandler<ReportsGenerateJobHandler>(services);
        RegisterHandler<MaintenanceCleanupJobHandler>(services);
        RegisterHandler<ExpiredDownloadsJobHandler>(services);
        RegisterHandler<InventoryTasksJobHandler>(services);
        RegisterHandler<PromotionsTasksJobHandler>(services);
        RegisterHandler<PluginTasksJobHandler>(services);

        services.AddHostedService<BackgroundJobProcessor>();
        return services;
    }

    public static IServiceCollection AddBackgroundJobHandler<THandler>(this IServiceCollection services)
        where THandler : class, IBackgroundJobHandler
    {
        services.AddScoped<IBackgroundJobHandler, THandler>();
        return services;
    }

    private static void RegisterHandler<THandler>(IServiceCollection services)
        where THandler : class, IBackgroundJobHandler
    {
        services.AddScoped<IBackgroundJobHandler, THandler>();
    }
}
