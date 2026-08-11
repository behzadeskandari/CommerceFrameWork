using Commerce.Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Media.Infrastructure.Persistence.Configurations;

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAsset");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(MediaAsset.FileNameMaxLength).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(MediaAsset.FileNameMaxLength).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(MediaAsset.ContentTypeMaxLength).IsRequired();
        builder.Property(x => x.Extension).HasMaxLength(MediaAsset.ExtensionMaxLength).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(MediaAsset.StorageKeyMaxLength).IsRequired();
        builder.Property(x => x.StorageProvider).HasMaxLength(MediaAsset.StorageProviderMaxLength).IsRequired();
        builder.Property(x => x.ThumbnailStorageKey).HasMaxLength(MediaAsset.StorageKeyMaxLength);
        builder.Property(x => x.AltText).HasMaxLength(MediaAsset.AltTextMaxLength);
        builder.Property(x => x.Title).HasMaxLength(MediaAsset.TitleMaxLength);
        builder.Property(x => x.ContentHash).HasMaxLength(MediaAsset.ContentHashMaxLength);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsPublic);
        builder.HasIndex(x => x.MediaType);
        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasIndex(x => x.ContentHash);
    }
}
