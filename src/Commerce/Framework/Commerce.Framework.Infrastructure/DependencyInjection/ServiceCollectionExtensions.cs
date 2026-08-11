using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Infrastructure.Configuration;
using Commerce.Framework.Infrastructure.Email;
using Commerce.Framework.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<ICommerceSettings, CommerceSettings>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();

        return services;
    }
}
