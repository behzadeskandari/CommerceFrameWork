using Commerce.Audit.Domain.Entities;
using Commerce.Audit.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Audit.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Action).HasMaxLength(AuditEntry.ActionMaxLength).IsRequired();
        builder.Property(x => x.ActorDisplay).HasMaxLength(AuditEntry.ActorDisplayMaxLength);
        builder.Property(x => x.ActorId).HasMaxLength(AuditEntry.ActorIdMaxLength);
        builder.Property(x => x.Category).HasConversion<int>().IsRequired();
        builder.Property(x => x.ActorType).HasConversion<int>().IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(AuditEntry.CorrelationIdMaxLength);
        builder.Property(x => x.DetailsJson).HasMaxLength(AuditEntry.DetailsJsonMaxLength);
        builder.Property(x => x.EntityId).HasMaxLength(AuditEntry.EntityIdMaxLength);
        builder.Property(x => x.EntityType).HasMaxLength(AuditEntry.EntityTypeMaxLength);
        builder.Property(x => x.EntryHash).HasMaxLength(AuditEntry.HashMaxLength).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(AuditEntry.IpAddressMaxLength);
        builder.Property(x => x.PreviousEntryHash).HasMaxLength(AuditEntry.HashMaxLength).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(AuditEntry.UserAgentMaxLength);
        builder.HasIndex(x => new { x.StoreId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.Category, x.OccurredAtUtc });
        builder.HasIndex(x => x.ActorId);
    }
}
