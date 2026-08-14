using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Orders.Infrastructure.Persistence.Configurations;

internal sealed class ReturnCaseConfiguration : IEntityTypeConfiguration<ReturnCase>
{
    public void Configure(EntityTypeBuilder<ReturnCase> builder)
    {
        builder.ToTable("ReturnCase");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(ReturnCase.ReasonMaxLength).IsRequired();
        builder.Property(x => x.CustomerNotes).HasMaxLength(ReturnCase.NotesMaxLength);
        builder.Property(x => x.AdminNotes).HasMaxLength(ReturnCase.NotesMaxLength);
        builder.Property(x => x.ReturnTrackingNumber).HasMaxLength(ReturnCase.TrackingNumberMaxLength);
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.RefundAmount).HasPrecision(18, 4);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ResolutionType).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.ReturnCaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ReturnCaseItemConfiguration : IEntityTypeConfiguration<ReturnCaseItem>
{
    public void Configure(EntityTypeBuilder<ReturnCaseItem> builder)
    {
        builder.ToTable("ReturnCaseItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RefundAmount).HasPrecision(18, 4);
        builder.HasIndex(x => x.ReturnCaseId);
        builder.HasIndex(x => x.OrderItemId);
    }
}
