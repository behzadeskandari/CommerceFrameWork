using Commerce.Audit.Infrastructure.DependencyInjection;
using Commerce.Audit.Infrastructure.Persistence;
using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Tests.Unit.Audit;

internal static class AuditTestComposition
{
    public static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<CommerceDataOptions>();
        services.AddSingleton<ICommerceModelContributor, AuditModelContributor>();
        services.AddSingleton<ICommerceDbContextConfigurator, InMemoryAuditDbContextConfigurator>();
        services.AddAuditInfrastructure(new ConfigurationBuilder().Build());
        services.AddCommerceDbContext();
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildSanitizerOnlyProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuditPublisher, NullAuditPublisher>();
        return services.BuildServiceProvider();
    }

    private sealed class InMemoryAuditDbContextConfigurator : ICommerceDbContextConfigurator
    {
        private readonly string _databaseName = Guid.NewGuid().ToString("N");

        public void Configure(DbContextOptionsBuilder optionsBuilder, CommerceDataOptions dataOptions) =>
            optionsBuilder.UseInMemoryDatabase(_databaseName);
    }
}
