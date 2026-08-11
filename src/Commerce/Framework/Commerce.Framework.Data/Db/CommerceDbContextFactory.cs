using Commerce.Framework.Data.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Data.Db;

public sealed class CommerceDbContextFactory : IDesignTimeDbContextFactory<CommerceDbContext>
{
    public CommerceDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var dataOptions = configuration.GetSection(CommerceDataOptions.SectionName).Get<CommerceDataOptions>()
            ?? new CommerceDataOptions
            {
                Provider = CommerceDatabaseProvider.SqlServer,
                ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=CommerceFramework;Trusted_Connection=True;TrustServerCertificate=True"
            };

        var configurator = new CommerceDbContextConfigurator();
        var optionsBuilder = new DbContextOptionsBuilder<CommerceDbContext>();
        configurator.Configure(optionsBuilder, dataOptions);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, CommerceModelCacheKeyFactory>();

        return new CommerceDbContext(optionsBuilder.Options, new ServiceCollection().BuildServiceProvider());
    }
}
