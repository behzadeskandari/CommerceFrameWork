using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Orders.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Order");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasMaxLength(Order.OrderNumberMaxLength).IsRequired();
        builder.Property(x => x.GuestEmail).HasMaxLength(Order.EmailMaxLength);
        builder.Property(x => x.CustomerEmail).HasMaxLength(Order.EmailMaxLength);
        builder.Property(x => x.CustomerDisplayName).HasMaxLength(Order.DisplayNameMaxLength);
        builder.Property(x => x.GuestAccessToken).HasMaxLength(Order.AccessTokenMaxLength);
        builder.Property(x => x.CurrencyCode).HasMaxLength(Order.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.PaymentStatus).IsRequired();
        builder.Property(x => x.FulfillmentStatus).IsRequired();
        builder.Property(x => x.SelectedShippingMethodId).HasMaxLength(Order.MethodIdMaxLength);
        builder.Property(x => x.SelectedShippingProviderSystemName).HasMaxLength(Order.ProviderSystemNameMaxLength);
        builder.Property(x => x.SelectedPaymentMethodId).HasMaxLength(Order.MethodIdMaxLength);
        builder.Property(x => x.SelectedPaymentMethodSystemName).HasMaxLength(Order.ProviderSystemNameMaxLength);
        builder.Property(x => x.Subtotal).HasPrecision(18, 4);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 4);
        builder.Property(x => x.ShippingTotal).HasPrecision(18, 4);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 4);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 4);
        builder.Property(x => x.StoreCreditApplied).HasPrecision(18, 4);
        builder.Property(x => x.GiftCardApplied).HasPrecision(18, 4);
        builder.Property(x => x.AppliedGiftCardCode).HasMaxLength(64);
        builder.Property(x => x.ReferralCode).HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.CheckoutId).IsUnique();
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.GuestEmail);
        builder.HasIndex(x => x.CustomerEmail);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.StoreId, x.Status, x.CreatedAtUtc });

        ConfigureAddress(builder.OwnsOne(x => x.BillingAddress), "Billing");
        ConfigureAddress(builder.OwnsOne(x => x.ShippingAddress), "Shipping");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.TaxLines)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.TaxLines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureAddress(OwnedNavigationBuilder<Order, OrderAddressSnapshot> address, string prefix)
    {
        address.Property(x => x.FirstName).HasMaxLength(OrderAddressSnapshot.NameMaxLength).HasColumnName($"{prefix}FirstName");
        address.Property(x => x.LastName).HasMaxLength(OrderAddressSnapshot.NameMaxLength).HasColumnName($"{prefix}LastName");
        address.Property(x => x.Country).HasMaxLength(OrderAddressSnapshot.CountryMaxLength).HasColumnName($"{prefix}Country");
        address.Property(x => x.StateProvince).HasMaxLength(OrderAddressSnapshot.StateProvinceMaxLength).HasColumnName($"{prefix}StateProvince");
        address.Property(x => x.City).HasMaxLength(OrderAddressSnapshot.CityMaxLength).HasColumnName($"{prefix}City");
        address.Property(x => x.Address1).HasMaxLength(OrderAddressSnapshot.AddressMaxLength).HasColumnName($"{prefix}Address1");
        address.Property(x => x.Address2).HasMaxLength(OrderAddressSnapshot.AddressMaxLength).HasColumnName($"{prefix}Address2");
        address.Property(x => x.PostalCode).HasMaxLength(OrderAddressSnapshot.PostalCodeMaxLength).HasColumnName($"{prefix}PostalCode");
        address.Property(x => x.PhoneNumber).HasMaxLength(OrderAddressSnapshot.PhoneMaxLength).HasColumnName($"{prefix}PhoneNumber");
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(400).IsRequired();
        builder.Property(x => x.VariantName).HasMaxLength(400);
        builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 4);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 4);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 4);
        builder.Property(x => x.LineTotal).HasPrecision(18, 4);
        builder.Property(x => x.PrimaryImageUrl).HasMaxLength(2000);
        builder.Property(x => x.PrimaryImageThumbnailUrl).HasMaxLength(2000);
        builder.Property(x => x.CancelledQuantity).HasDefaultValue(0);
        builder.Property(x => x.ReturnedQuantity).HasDefaultValue(0);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.OfferId);
        builder.HasIndex(x => x.ProductId);
    }
}

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasMaxLength(OrderStatusHistory.StatusMaxLength);
        builder.Property(x => x.ToStatus).HasMaxLength(OrderStatusHistory.StatusMaxLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(OrderStatusHistory.ReasonMaxLength).IsRequired();
        builder.Property(x => x.Actor).HasMaxLength(OrderStatusHistory.ActorMaxLength);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => x.OrderId);
    }
}

internal sealed class OrderCreationIdempotencyConfiguration : IEntityTypeConfiguration<OrderCreationIdempotency>
{
    public void Configure(EntityTypeBuilder<OrderCreationIdempotency> builder)
    {
        builder.ToTable("OrderCreationIdempotency");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(OrderCreationIdempotency.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => x.CheckoutId);
        builder.HasIndex(x => x.OrderId);
    }
}

internal sealed class OrderTaxLineConfiguration : IEntityTypeConfiguration<OrderTaxLine>
{
    public void Configure(EntityTypeBuilder<OrderTaxLine> builder)
    {
        builder.ToTable("OrderTaxLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.TaxCategoryName).HasMaxLength(200);
        builder.Property(x => x.RatePercentage).HasPrecision(18, 4);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
        builder.HasIndex(x => x.OrderId);
    }
}

internal sealed class StoreOrderNumberSequenceConfiguration : IEntityTypeConfiguration<StoreOrderNumberSequence>
{
    public void Configure(EntityTypeBuilder<StoreOrderNumberSequence> builder)
    {
        builder.ToTable("StoreOrderNumberSequence");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.StoreId, x.Year }).IsUnique();
    }
}
