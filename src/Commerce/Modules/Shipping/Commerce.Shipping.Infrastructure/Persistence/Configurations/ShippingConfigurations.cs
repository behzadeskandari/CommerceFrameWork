using Commerce.Shipping.Domain.Entities;
using Commerce.Shipping.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Shipping.Infrastructure.Persistence.Configurations;

internal sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("ShippingMethod");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(ShippingMethod.NameMaxLength).IsRequired();
        builder.Property(x => x.SystemName).HasMaxLength(ShippingMethod.SystemNameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(ShippingMethod.DescriptionMaxLength);
        builder.Property(x => x.ProviderSystemName).HasMaxLength(ShippingMethod.ProviderSystemNameMaxLength).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.IsActive);
    }
}

internal sealed class ShippingZoneConfiguration : IEntityTypeConfiguration<ShippingZone>
{
    public void Configure(EntityTypeBuilder<ShippingZone> builder)
    {
        builder.ToTable("ShippingZone");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(ShippingZone.NameMaxLength).IsRequired();
        builder.Property(x => x.SystemName).HasMaxLength(ShippingZone.SystemNameMaxLength).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();
        builder.HasIndex(x => x.StoreId);

        builder.HasMany(x => x.Countries).WithOne().HasForeignKey(x => x.ShippingZoneId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Countries).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.States).WithOne().HasForeignKey(x => x.ShippingZoneId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.States).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.PostalRules).WithOne().HasForeignKey(x => x.ShippingZoneId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.PostalRules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ShippingZoneCountryConfiguration : IEntityTypeConfiguration<ShippingZoneCountry>
{
    public void Configure(EntityTypeBuilder<ShippingZoneCountry> builder)
    {
        builder.ToTable("ShippingZoneCountry");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.HasIndex(x => new { x.ShippingZoneId, x.CountryCode });
    }
}

internal sealed class ShippingZoneStateConfiguration : IEntityTypeConfiguration<ShippingZoneState>
{
    public void Configure(EntityTypeBuilder<ShippingZoneState> builder)
    {
        builder.ToTable("ShippingZoneState");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.StateProvince).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.ShippingZoneId, x.CountryCode, x.StateProvince });
    }
}

internal sealed class ShippingZonePostalRuleConfiguration : IEntityTypeConfiguration<ShippingZonePostalRule>
{
    public void Configure(EntityTypeBuilder<ShippingZonePostalRule> builder)
    {
        builder.ToTable("ShippingZonePostalRule");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.PostalFrom).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PostalTo).HasMaxLength(32);
        builder.HasIndex(x => new { x.ShippingZoneId, x.CountryCode });
    }
}

internal sealed class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
{
    public void Configure(EntityTypeBuilder<ShippingRate> builder)
    {
        builder.ToTable("ShippingRate");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.BasePrice).HasPrecision(18, 4);
        builder.Property(x => x.PricePerWeightUnit).HasPrecision(18, 4);
        builder.Property(x => x.PricePerQuantityUnit).HasPrecision(18, 4);
        builder.Property(x => x.OrderSubtotalPercentage).HasPrecision(18, 4);
        builder.Property(x => x.FreeShippingThreshold).HasPrecision(18, 4);
        builder.Property(x => x.MinOrderSubtotal).HasPrecision(18, 4);
        builder.Property(x => x.MaxOrderSubtotal).HasPrecision(18, 4);
        builder.Property(x => x.MinWeightGrams).HasPrecision(18, 4);
        builder.Property(x => x.MaxWeightGrams).HasPrecision(18, 4);
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.ShippingMethodId);
        builder.HasIndex(x => x.ShippingZoneId);
    }
}
