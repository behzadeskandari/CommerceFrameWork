using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Framework.Plugins.Persistence;

public sealed class CommercePluginStoreConfiguration
{
    public int Id { get; private set; }

    public string PluginSystemName { get; private set; } = string.Empty;

    public int StoreId { get; private set; }

    public bool IsEnabled { get; private set; }

    public string? ConfigurationJson { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static CommercePluginStoreConfiguration Create(string pluginSystemName, int storeId, bool isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginSystemName);

        return new CommercePluginStoreConfiguration
        {
            PluginSystemName = pluginSystemName,
            StoreId = storeId,
            IsEnabled = isEnabled,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetConfiguration(string? configurationJson)
    {
        ConfigurationJson = configurationJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class CommercePluginStoreConfigurationConfiguration : IEntityTypeConfiguration<CommercePluginStoreConfiguration>
{
    public void Configure(EntityTypeBuilder<CommercePluginStoreConfiguration> builder)
    {
        builder.ToTable("CommercePluginStoreConfigurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PluginSystemName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConfigurationJson);
        builder.HasIndex(x => new { x.PluginSystemName, x.StoreId }).IsUnique();
    }
}
