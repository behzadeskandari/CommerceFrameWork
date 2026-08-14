using Commerce.Shipping.Domain.Entities;
using Commerce.Shipping.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Shipping.Infrastructure.Persistence.Configurations;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderSystemName).HasMaxLength(Shipment.ProviderSystemNameMaxLength);
        builder.Property(x => x.TrackingNumber).HasMaxLength(Shipment.TrackingNumberMaxLength);
        builder.Property(x => x.TrackingUrl).HasMaxLength(Shipment.TrackingUrlMaxLength);
        builder.Property(x => x.CarrierName).HasMaxLength(Shipment.CarrierNameMaxLength);
        builder.Property(x => x.Notes).HasMaxLength(Shipment.NotesMaxLength);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.StoreId);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("ShipmentItem");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderItemId);
        builder.HasIndex(x => x.ShipmentId);
    }
}
