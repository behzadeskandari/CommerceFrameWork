using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Application.DependencyInjection;
using Commerce.DisasterRecovery.Application.Services;
using Commerce.DisasterRecovery.Infrastructure.Backup;
using Commerce.DisasterRecovery.Infrastructure.Health;
using Commerce.DisasterRecovery.Infrastructure.Jobs;
using Commerce.Framework.Scheduling;
using Commerce.DisasterRecovery.Infrastructure.Migrations;
using Commerce.DisasterRecovery.Infrastructure.Persistence;
using Commerce.DisasterRecovery.Infrastructure.Persistence.Repositories;
using Commerce.DisasterRecovery.Infrastructure.Security;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Commerce.DisasterRecovery.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDisasterRecoveryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DisasterRecoveryInfrastructureOptions>(configuration.GetSection(DisasterRecoveryInfrastructureOptions.SectionName));
        services.Configure<DisasterRecoveryApplicationOptions>(configuration.GetSection(DisasterRecoveryApplicationOptions.SectionName));
        services.AddSingleton<ICommerceModelContributor, DisasterRecoveryModelContributor>();
        services.AddSingleton<ICommerceMigration, DisasterRecoveryInitialMigration>();
        services.AddSingleton<IModulePermissionContributor, DisasterRecoveryPermissionContributor>();
        services.Replace(ServiceDescriptor.Singleton<IBackupHealthProbe, BackupHealthProbe>());
        services.AddScoped<IBackupRepository, EfBackupRepository>();
        services.AddScoped<IBackupComponentCollector, BackupComponentCollector>();
        services.AddScoped<ISqlServerDatabaseBackupProvider, SqlServerDatabaseBackupProvider>();
        services.AddScoped<ISqlServerBackupVerifier, SqlServerBackupVerifier>();
        services.AddScoped<IDataIntegrityProbe, DataIntegrityProbe>();
        services.AddScoped<IBackgroundJobHandler, BackupCreateJobHandler>();
        services.AddScoped<IBackgroundJobHandler, BackupRetentionJobHandler>();
        services.AddDisasterRecoveryApplication();
        return services;
    }
}

public static class DisasterRecoveryStartupExtensions
{
    public static async Task RegisterDisasterRecoveryRecurringJobsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var options = configuration.GetSection(DisasterRecoveryInfrastructureOptions.SectionName).Get<DisasterRecoveryInfrastructureOptions>()
            ?? new DisasterRecoveryInfrastructureOptions();
        if (!options.EnableScheduledBackups)
        {
            return;
        }

        var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();
        await scheduler.RegisterRecurringAsync(
            new RegisterRecurringJobRequest("backup.create", BackgroundJobTypes.BackupCreate, 86400),
            cancellationToken).ConfigureAwait(false);
        await scheduler.RegisterRecurringAsync(
            new RegisterRecurringJobRequest("backup.retention", BackgroundJobTypes.BackupRetention, 86400),
            cancellationToken).ConfigureAwait(false);
    }
}
