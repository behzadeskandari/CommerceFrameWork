using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Data.Installation;

public sealed class DynamicCommerceDbContextConfigurator(
    IInstallationConnectionProvider connectionProvider,
    IOptionsMonitor<CommerceDataOptions> dataOptionsMonitor) : ICommerceDbContextConfigurator
{
    public const string InMemoryConnectionToken = "__InMemory__";

    private readonly CommerceDbContextConfigurator _fallback = new();

    public void Configure(DbContextOptionsBuilder optionsBuilder, CommerceDataOptions dataOptions)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        var runtime = connectionProvider.GetCurrent();
        if (IsInMemory(runtime.ConnectionString))
        {
            var databaseName = string.IsNullOrWhiteSpace(runtime.ConnectionString)
                ? InMemoryConnectionToken
                : runtime.ConnectionString;
            optionsBuilder.UseInMemoryDatabase(databaseName);
            return;
        }

        if (!string.IsNullOrWhiteSpace(runtime.ConnectionString))
        {
            var effective = new CommerceDataOptions
            {
                Provider = runtime.Provider,
                ConnectionString = runtime.ConnectionString,
                CommandTimeoutSeconds = dataOptions.CommandTimeoutSeconds
            };

            _fallback.Configure(optionsBuilder, effective);
            return;
        }

        if (!string.IsNullOrWhiteSpace(dataOptionsMonitor.CurrentValue.ConnectionString))
        {
            _fallback.Configure(optionsBuilder, dataOptionsMonitor.CurrentValue);
            return;
        }

        if (!string.IsNullOrWhiteSpace(dataOptions.ConnectionString))
        {
            _fallback.Configure(optionsBuilder, dataOptions);
        }
    }

    internal static bool IsInMemory(string? connectionString) =>
        !string.IsNullOrWhiteSpace(connectionString) &&
        connectionString.StartsWith(InMemoryConnectionToken, StringComparison.Ordinal);
}
