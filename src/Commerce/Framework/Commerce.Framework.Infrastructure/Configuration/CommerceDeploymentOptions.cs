namespace Commerce.Framework.Infrastructure.Configuration;

public sealed class CommerceDeploymentOptions
{
    public const string SectionName = "Commerce:Deployment";

    /// <summary>
    /// When true and commerce is installed, pending module migrations run on host startup.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>
    /// Maximum seconds to wait for the database to accept connections before startup continues.
    /// Only applies after commerce is installed.
    /// </summary>
    public int WaitForDatabaseSeconds { get; set; }

    /// <summary>
    /// Delay between database connection attempts during startup wait.
    /// </summary>
    public int DatabaseRetryDelaySeconds { get; set; } = 3;
}
