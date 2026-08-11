namespace Commerce.Framework.Data.Configuration;

public sealed class CommerceDataOptions
{
    public const string SectionName = "Commerce:Database";

    public CommerceDatabaseProvider Provider { get; set; } = CommerceDatabaseProvider.SqlServer;

    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
