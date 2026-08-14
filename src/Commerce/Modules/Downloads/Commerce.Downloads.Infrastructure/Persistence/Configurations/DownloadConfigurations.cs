using Commerce.Downloads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Downloads.Infrastructure.Persistence.Configurations;

public sealed class ProductDownloadSettingsConfiguration : IEntityTypeConfiguration<ProductDownloadSettings>
{
    public void Configure(EntityTypeBuilder<ProductDownloadSettings> builder)
    {
        builder.ToTable("ProductDownloadSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.StoreId).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.StoreId }).IsUnique();
    }
}

public sealed class ProductDownloadFileConfiguration : IEntityTypeConfiguration<ProductDownloadFile>
{
    public void Configure(EntityTypeBuilder<ProductDownloadFile> builder)
    {
        builder.ToTable("ProductDownloadFiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).HasMaxLength(ProductDownloadFile.DisplayNameMaxLength);
        builder.HasIndex(x => new { x.ProductId, x.StoreId, x.MediaAssetId }).IsUnique();
    }
}

public sealed class DownloadEntitlementConfiguration : IEntityTypeConfiguration<DownloadEntitlement>
{
    public void Configure(EntityTypeBuilder<DownloadEntitlement> builder)
    {
        builder.ToTable("DownloadEntitlements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GuestAccessToken).HasMaxLength(128);
        builder.HasIndex(x => x.OrderItemId).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.StoreId });
    }
}

public sealed class DownloadHistoryEntryConfiguration : IEntityTypeConfiguration<DownloadHistoryEntry>
{
    public void Configure(EntityTypeBuilder<DownloadHistoryEntry> builder)
    {
        builder.ToTable("DownloadHistoryEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IpAddress).HasMaxLength(DownloadHistoryEntry.IpAddressMaxLength);
        builder.Property(x => x.UserAgent).HasMaxLength(DownloadHistoryEntry.UserAgentMaxLength);
        builder.Property(x => x.FailureReason).HasMaxLength(DownloadHistoryEntry.FailureReasonMaxLength);
        builder.HasIndex(x => x.EntitlementId);
    }
}
