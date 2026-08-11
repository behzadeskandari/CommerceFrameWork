using System.Text.Json;
using Commerce.Framework.Data.Configuration;
using Microsoft.Extensions.Hosting;

namespace Commerce.Framework.Data.Installation;

public sealed class FileInstallationConnectionProvider(IHostEnvironment hostEnvironment) : IInstallationConnectionProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly object _sync = new();
    private InstallationConnectionOptions _current = new();

    public InstallationConnectionOptions GetCurrent()
    {
        lock (_sync)
        {
            return new InstallationConnectionOptions
            {
                Provider = _current.Provider,
                ConnectionString = _current.ConnectionString
            };
        }
    }

    public void SetPending(CommerceDatabaseProvider provider, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        lock (_sync)
        {
            _current = new InstallationConnectionOptions
            {
                Provider = provider,
                ConnectionString = connectionString
            };
        }
    }

    public async Task PersistAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InstallationConnectionOptions snapshot;
        lock (_sync)
        {
            snapshot = new InstallationConnectionOptions
            {
                Provider = _current.Provider,
                ConnectionString = _current.ConnectionString
            };
        }

        if (string.IsNullOrWhiteSpace(snapshot.ConnectionString))
        {
            throw new InvalidOperationException("Cannot persist an empty database connection.");
        }

        var directory = Path.Combine(hostEnvironment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);

        var payload = new PersistedConnectionFile
        {
            Provider = snapshot.Provider.ToString(),
            ConnectionString = snapshot.ConnectionString
        };

        var path = GetFilePath(hostEnvironment.ContentRootPath);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
    }

    public Task LoadPersistedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetFilePath(hostEnvironment.ContentRootPath);
        if (!File.Exists(path))
        {
            return Task.CompletedTask;
        }

        var json = File.ReadAllText(path);
        var payload = JsonSerializer.Deserialize<PersistedConnectionFile>(json);
        if (payload is null || string.IsNullOrWhiteSpace(payload.ConnectionString))
        {
            return Task.CompletedTask;
        }

        if (!Enum.TryParse<CommerceDatabaseProvider>(payload.Provider, true, out var provider))
        {
            provider = CommerceDatabaseProvider.SqlServer;
        }

        lock (_sync)
        {
            _current = new InstallationConnectionOptions
            {
                Provider = provider,
                ConnectionString = payload.ConnectionString
            };
        }

        return Task.CompletedTask;
    }

    internal static string GetFilePath(string contentRootPath) =>
        Path.Combine(contentRootPath, "App_Data", "commerce.database.json");

    private sealed class PersistedConnectionFile
    {
        public string Provider { get; set; } = "SqlServer";

        public string ConnectionString { get; set; } = string.Empty;
    }
}
