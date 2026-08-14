using Commerce.Seo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Seo.Infrastructure.Persistence.Configurations;

public sealed class UrlRecordConfiguration : IEntityTypeConfiguration<UrlRecord>
{
    public void Configure(EntityTypeBuilder<UrlRecord> builder)
    {
        builder.ToTable("UrlRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityName).HasMaxLength(UrlRecord.EntityNameMaxLength).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(UrlRecord.SlugMaxLength).IsRequired();
        builder.HasIndex(x => new { x.Slug, x.LanguageId, x.StoreId }).IsUnique();
        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.LanguageId, x.StoreId }).IsUnique();
    }
}

public sealed class SeoMetadataConfiguration : IEntityTypeConfiguration<SeoMetadata>
{
    public void Configure(EntityTypeBuilder<SeoMetadata> builder)
    {
        builder.ToTable("SeoMetadata");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityName).HasMaxLength(SeoMetadata.EntityNameMaxLength).IsRequired();
        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.LanguageId, x.StoreId }).IsUnique();
    }
}

public sealed class SeoSettingsConfiguration : IEntityTypeConfiguration<SeoSettings>
{
    public void Configure(EntityTypeBuilder<SeoSettings> builder)
    {
        builder.ToTable("SeoSettings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.StoreId).IsUnique();
    }
}
