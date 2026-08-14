using Commerce.Reviews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Reviews.Infrastructure.Persistence.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(ProductReview.TitleMaxLength).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(ProductReview.ContentMaxLength).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.CustomerId, x.StoreId }).IsUnique();
        builder.HasIndex(x => new { x.StoreId, x.ProductId, x.ModerationStatus });
        builder.HasIndex(x => new { x.CustomerId, x.StoreId });
    }
}

public sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CustomerId, x.StoreId }).IsUnique();
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.WishlistId, x.ProductId }).IsUnique();
    }
}
