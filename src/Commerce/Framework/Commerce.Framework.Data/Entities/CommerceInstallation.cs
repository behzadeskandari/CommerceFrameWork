namespace Commerce.Framework.Data.Entities;

public sealed class CommerceInstallation
{
    public int Id { get; set; }

    public Guid InstallationId { get; set; }

    public string Status { get; set; } = "NotInstalled";

    public int CurrentStep { get; set; }

    public string? ApplicationVersion { get; set; }

    public string? InstalledVersion { get; set; }

    public DateTime? InstalledAtUtc { get; set; }

    public string? LastError { get; set; }
}
