using Commerce.Framework.Data.Configuration;

namespace Commerce.Framework.Data.Installation;

public sealed class InstallationConnectionOptions
{
    public CommerceDatabaseProvider Provider { get; set; } = CommerceDatabaseProvider.SqlServer;

    public string? ConnectionString { get; set; }
}

public interface IInstallationConnectionProvider
{
    InstallationConnectionOptions GetCurrent();

    void SetPending(CommerceDatabaseProvider provider, string connectionString);

    Task PersistAsync(CancellationToken cancellationToken = default);

    Task LoadPersistedAsync(CancellationToken cancellationToken = default);
}
