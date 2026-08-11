using Commerce.Framework.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Framework.Data.Configuration;

public interface ICommerceDbContextConfigurator
{
    void Configure(DbContextOptionsBuilder optionsBuilder, CommerceDataOptions dataOptions);
}

public sealed class CommerceDbContextConfigurator : ICommerceDbContextConfigurator
{
    public void Configure(DbContextOptionsBuilder optionsBuilder, CommerceDataOptions dataOptions)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(dataOptions);

        if (string.IsNullOrWhiteSpace(dataOptions.ConnectionString))
        {
            throw new InvalidOperationException("Commerce database connection string is not configured.");
        }

        optionsBuilder = optionsBuilder.EnableSensitiveDataLogging(false);

        switch (dataOptions.Provider)
        {
            case CommerceDatabaseProvider.SqlServer:
                optionsBuilder.UseSqlServer(
                    dataOptions.ConnectionString,
                    sql => sql.CommandTimeout(dataOptions.CommandTimeoutSeconds));
                break;

            case CommerceDatabaseProvider.PostgreSql:
                throw new NotSupportedException(
                    "PostgreSQL support is planned for a future phase. Configure Provider=SqlServer for Phase 1.");

            default:
                throw new InvalidOperationException($"Unsupported database provider: {dataOptions.Provider}");
        }
    }
}
