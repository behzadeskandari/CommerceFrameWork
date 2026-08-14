using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OnHand).IsRequired();
        builder.Property(x => x.Reserved).IsRequired();
        builder.Property(x => x.Incoming).IsRequired();
        builder.Property(x => x.TrackInventory).IsRequired();
        builder.Property(x => x.AllowBackorder).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.StoreId, x.OfferId, x.WarehouseId }).IsUnique();
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.OfferId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.StockLocationId);

        builder.HasMany(x => x.Movements)
            .WithOne()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Movements).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Reservations)
            .WithOne()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Reservations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovement");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(InventoryMovement.ReasonMaxLength).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(InventoryMovement.CreatedByMaxLength);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => x.InventoryItemId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReleaseReason).HasMaxLength(InventoryReservation.ReasonMaxLength);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.HasIndex(x => x.InventoryItemId);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouse");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Warehouse.NameMaxLength).IsRequired();
        builder.Property(x => x.SystemName).HasMaxLength(Warehouse.SystemNameMaxLength).IsRequired();
        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();
        builder.HasIndex(x => x.StoreId);
    }
}

internal sealed class StockLocationConfiguration : IEntityTypeConfiguration<StockLocation>
{
    public void Configure(EntityTypeBuilder<StockLocation> builder)
    {
        builder.ToTable("StockLocation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(StockLocation.CodeMaxLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(StockLocation.NameMaxLength).IsRequired();
        builder.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
    }
}
