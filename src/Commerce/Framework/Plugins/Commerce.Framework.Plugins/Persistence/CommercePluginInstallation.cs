using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Framework.Plugins.Persistence;

public sealed class CommercePluginInstallation
{
    public int Id { get; private set; }

    public string SystemName { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public string InstalledVersion { get; private set; } = string.Empty;

    public bool IsInstalled { get; private set; }

    public bool IsEnabled { get; private set; }

    public string Status { get; private set; } = "Installed";

    public DateTimeOffset InstalledAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? LastError { get; private set; }

    public string? Configuration { get; private set; }

    public static CommercePluginInstallation Create(string systemName, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var now = DateTimeOffset.UtcNow;
        return new CommercePluginInstallation
        {
            SystemName = systemName,
            Version = version,
            InstalledVersion = version,
            IsInstalled = true,
            IsEnabled = false,
            Status = "Installed",
            InstalledAt = now,
            UpdatedAt = now
        };
    }

    public void Enable()
    {
        IsEnabled = true;
        Status = "Enabled";
        UpdatedAt = DateTimeOffset.UtcNow;
        LastError = null;
    }

    public void Disable()
    {
        IsEnabled = false;
        Status = "Disabled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        LastError = error;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateVersion(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        Version = version;
        InstalledVersion = version;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class CommercePluginInstallationConfiguration : IEntityTypeConfiguration<CommercePluginInstallation>
{
    public void Configure(EntityTypeBuilder<CommercePluginInstallation> builder)
    {
        builder.ToTable("CommercePluginInstallations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.InstalledVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.Configuration);
        builder.HasIndex(x => x.SystemName).IsUnique();
    }
}
