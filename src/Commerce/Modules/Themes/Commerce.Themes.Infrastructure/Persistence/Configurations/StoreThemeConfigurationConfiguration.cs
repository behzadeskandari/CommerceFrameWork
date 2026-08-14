using Commerce.Framework.Data.Db;
using Commerce.Themes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Themes.Infrastructure.Persistence.Configurations;

public sealed class StoreThemeConfigurationConfiguration : IEntityTypeConfiguration<StoreThemeConfiguration>
{
    public void Configure(EntityTypeBuilder<StoreThemeConfiguration> builder)
    {
        builder.ToTable("StoreThemeConfigurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StoreId).IsRequired();
        builder.HasIndex(x => x.StoreId).IsUnique();
        builder.Property(x => x.ThemeSystemName).HasMaxLength(StoreThemeConfiguration.ThemeSystemNameMaxLength).IsRequired();
        builder.Property(x => x.ConfigurationJson).HasMaxLength(StoreThemeConfiguration.JsonMaxLength).IsRequired();
        builder.Property(x => x.LayoutOverridesJson).HasMaxLength(StoreThemeConfiguration.JsonMaxLength).IsRequired();
    }
}
