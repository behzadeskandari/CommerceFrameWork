using Commerce.DisasterRecovery.Application.Services;
using Commerce.DisasterRecovery.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.DisasterRecovery.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDisasterRecoveryApplication(this IServiceCollection services)
    {
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IBackupVerificationService, BackupVerificationService>();
        services.AddScoped<IRecoveryTestService, RecoveryTestService>();
        services.AddScoped<IDataIntegrityService, DataIntegrityService>();
        services.AddSingleton<IDisasterRecoveryMetadataService, DisasterRecoveryMetadataService>();
        return services;
    }
}
