using Commerce.Checkout.Domain.Entities;
using Commerce.Checkout.Domain.Enums;
using Commerce.Checkout.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Checkout.Infrastructure.Persistence.Configurations;

internal sealed class CheckoutSessionConfiguration : IEntityTypeConfiguration<CheckoutSession>
{
    public void Configure(EntityTypeBuilder<CheckoutSession> builder)
    {
        builder.ToTable("CheckoutSession");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GuestToken).HasMaxLength(CheckoutSession.GuestTokenMaxLength);
        builder.Property(x => x.GuestEmail).HasMaxLength(CheckoutSession.EmailMaxLength);
        builder.Property(x => x.CurrencyCode).HasMaxLength(CheckoutSession.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.SelectedShippingMethodId).HasMaxLength(CheckoutSession.MethodIdMaxLength);
        builder.Property(x => x.SelectedShippingProviderSystemName).HasMaxLength(CheckoutSession.ProviderSystemNameMaxLength);
        builder.Property(x => x.SelectedPaymentMethodId).HasMaxLength(CheckoutSession.MethodIdMaxLength);
        builder.Property(x => x.SelectedPaymentMethodSystemName).HasMaxLength(CheckoutSession.ProviderSystemNameMaxLength);
        builder.Property(x => x.Subtotal).HasPrecision(18, 4);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 4);
        builder.Property(x => x.ShippingTotal).HasPrecision(18, 4);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 4);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 4);
        builder.Property(x => x.SelectedShippingPrice).HasPrecision(18, 4);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.CartUpdatedAtUtc).IsRequired();
        builder.Property(x => x.AppliedCouponCode).HasMaxLength(64);
        builder.Property(x => x.AppliedGiftCardCode).HasMaxLength(64);
        builder.Property(x => x.AppliedStoreCreditAmount).HasPrecision(18, 4);
        builder.Property(x => x.GiftCardApplied).HasPrecision(18, 4);
        builder.Property(x => x.StoreCreditApplied).HasPrecision(18, 4);
        builder.Property(x => x.ReferralCode).HasMaxLength(64);

        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CartId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.GuestToken);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CartId)
            .HasFilter($"[{nameof(CheckoutSession.Status)}] IN ({(int)CheckoutStatus.Active}, {(int)CheckoutStatus.RequiresReview}, {(int)CheckoutStatus.ReadyForOrder})")
            .IsUnique();

        ConfigureAddress(builder.OwnsOne(x => x.BillingAddress), "Billing");
        ConfigureAddress(builder.OwnsOne(x => x.ShippingAddress), "Shipping");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.CheckoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureAddress(OwnedNavigationBuilder<CheckoutSession, CheckoutAddressSnapshot> address, string prefix)
    {
        address.Property(x => x.FirstName).HasMaxLength(CheckoutAddressSnapshot.NameMaxLength).HasColumnName($"{prefix}FirstName");
        address.Property(x => x.LastName).HasMaxLength(CheckoutAddressSnapshot.NameMaxLength).HasColumnName($"{prefix}LastName");
        address.Property(x => x.Country).HasMaxLength(CheckoutAddressSnapshot.CountryMaxLength).HasColumnName($"{prefix}Country");
        address.Property(x => x.StateProvince).HasMaxLength(CheckoutAddressSnapshot.StateProvinceMaxLength).HasColumnName($"{prefix}StateProvince");
        address.Property(x => x.City).HasMaxLength(CheckoutAddressSnapshot.CityMaxLength).HasColumnName($"{prefix}City");
        address.Property(x => x.Address1).HasMaxLength(CheckoutAddressSnapshot.AddressMaxLength).HasColumnName($"{prefix}Address1");
        address.Property(x => x.Address2).HasMaxLength(CheckoutAddressSnapshot.AddressMaxLength).HasColumnName($"{prefix}Address2");
        address.Property(x => x.PostalCode).HasMaxLength(CheckoutAddressSnapshot.PostalCodeMaxLength).HasColumnName($"{prefix}PostalCode");
        address.Property(x => x.PhoneNumber).HasMaxLength(CheckoutAddressSnapshot.PhoneMaxLength).HasColumnName($"{prefix}PhoneNumber");
        address.Property(x => x.SourceCustomerAddressId).HasColumnName($"{prefix}SourceCustomerAddressId");
    }
}

internal sealed class CheckoutSessionItemConfiguration : IEntityTypeConfiguration<CheckoutSessionItem>
{
    public void Configure(EntityTypeBuilder<CheckoutSessionItem> builder)
    {
        builder.ToTable("CheckoutSessionItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(CheckoutSession.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 4);
        builder.Property(x => x.PreviousUnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.CheckoutSessionId);
        builder.HasIndex(x => x.CartItemId);
        builder.HasIndex(x => x.OfferId);
    }
}
