namespace Commerce.Framework.Data.Migrations;

public sealed class MigrationVersionInfo
{
    public int Id { get; set; }

    public string Version { get; set; } = null!;

    public string MigrationName { get; set; } = null!;

    public string Module { get; set; } = null!;

    public DateTime AppliedAt { get; set; }
}
