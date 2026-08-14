using Commerce.Pricing.Domain.Entities;
using Commerce.Pricing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Pricing.Infrastructure.Persistence.Configurations;

internal sealed class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discount");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Discount.NameMaxLength).IsRequired();
        builder.Property(x => x.SystemName).HasMaxLength(Discount.SystemNameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Discount.DescriptionMaxLength);
        builder.Property(x => x.CurrencyCode).HasMaxLength(Discount.CurrencyCodeMaxLength);
        builder.Property(x => x.Value).HasPrecision(18, 4);
        builder.Property(x => x.MaximumDiscountAmount).HasPrecision(18, 4);
        builder.Property(x => x.MinimumCartSubtotal).HasPrecision(18, 4);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.SystemName).IsUnique();
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => new { x.IsActive, x.StartsAtUtc, x.EndsAtUtc });
        builder.HasIndex(x => x.Priority);

        builder.HasIndex(x => x.CustomerGroupId);

        builder.HasMany(x => x.Targets)
            .WithOne()
            .HasForeignKey(x => x.DiscountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Targets).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class DiscountTargetConfiguration : IEntityTypeConfiguration<DiscountTarget>
{
    public void Configure(EntityTypeBuilder<DiscountTarget> builder)
    {
        builder.ToTable("DiscountTarget");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DiscountId, x.TargetType, x.TargetId });
        builder.HasIndex(x => new { x.TargetType, x.TargetId });
    }
}

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupon");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(Coupon.CodeMaxLength).IsRequired();
        builder.Property(x => x.UsageCount).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.DiscountId);
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => new { x.IsActive, x.StartsAtUtc, x.EndsAtUtc });
    }
}

internal sealed class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("CouponUsage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UsedAtUtc).IsRequired();
        builder.HasIndex(x => x.CouponId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.CouponId, x.CustomerId });
        builder.HasIndex(x => new { x.CouponId, x.CustomerId, x.OrderId }).IsUnique();
    }
}

internal sealed class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable("PricingCustomerGroup");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(CustomerGroup.NameMaxLength).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(CustomerGroup.CodeMaxLength).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.Code }).IsUnique();
        builder.HasIndex(x => x.StoreId);
    }
}

internal sealed class CustomerGroupPriceConfiguration : IEntityTypeConfiguration<CustomerGroupPrice>
{
    public void Configure(EntityTypeBuilder<CustomerGroupPrice> builder)
    {
        builder.ToTable("PricingCustomerGroupPrice");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 4);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.CustomerGroupId);
        builder.HasIndex(x => new { x.CustomerGroupId, x.StoreId, x.ProductId, x.VariantId, x.CurrencyId }).IsUnique();
    }
}
