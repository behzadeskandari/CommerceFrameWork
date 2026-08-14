using Commerce.Cart.Domain.Entities;
using Commerce.Cart.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Cart.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("Cart");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GuestToken).HasMaxLength(ShoppingCart.GuestTokenMaxLength);
        builder.Property(x => x.CurrencyCode).HasMaxLength(ShoppingCart.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.AppliedCouponCode).HasMaxLength(64);

        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.GuestToken);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.StoreId, x.CustomerId, x.CurrencyId, x.Status })
            .HasFilter("[CustomerId] IS NOT NULL AND [Status] = 0")
            .IsUnique();
        builder.HasIndex(x => new { x.StoreId, x.GuestToken, x.CurrencyId, x.Status })
            .HasFilter("[GuestToken] IS NOT NULL AND [Status] = 0")
            .IsUnique();

        builder.HasMany<CartItem>()
            .WithOne()
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OfferId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.CartId);
        builder.HasIndex(x => x.OfferId);
        builder.HasIndex(x => new { x.CartId, x.OfferId }).IsUnique();
    }
}
