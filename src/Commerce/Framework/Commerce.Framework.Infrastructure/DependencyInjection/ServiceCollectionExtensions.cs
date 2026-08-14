using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Infrastructure.Audit;
using Commerce.Framework.Infrastructure.Configuration;
using Commerce.Framework.Infrastructure.Email;
using Commerce.Framework.Infrastructure.Observability;
using Commerce.Framework.Infrastructure.Security;
using Commerce.Framework.Infrastructure.Sms;
using Commerce.Framework.Contracts.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Commerce.Framework.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommerceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CommerceOptions>(configuration.GetSection(CommerceOptions.SectionName));
        services.Configure<CommerceDeploymentOptions>(configuration.GetSection(CommerceDeploymentOptions.SectionName));
        services.AddSingleton<ICommerceSettings, CommerceSettings>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<ISmsSender, LoggingSmsSender>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<IAuditPublisher, NullAuditPublisher>();
        services.TryAddSingleton<ICorrelationContext, NullCorrelationContext>();
        services.TryAddSingleton<ISchedulingHealthProbe, NullSchedulingHealthProbe>();
        services.TryAddSingleton<IPaymentProviderHealthProbe, NullPaymentProviderHealthProbe>();
        services.TryAddSingleton<ICacheHealthProbe, NullCacheHealthProbe>();
        services.TryAddSingleton<IBackupHealthProbe, NullBackupHealthProbe>();

        return services;
    }
}
