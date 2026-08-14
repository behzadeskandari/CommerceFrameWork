using Commerce.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Payments.Infrastructure.Persistence.Configurations;

internal sealed class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable("GiftCard");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(GiftCard.CodeMaxLength).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(GiftCard.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.InitialAmount).HasPrecision(18, 4);
        builder.Property(x => x.Balance).HasPrecision(18, 4);
        builder.Property(x => x.RecipientEmail).HasMaxLength(GiftCard.EmailMaxLength);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.StoreId);
        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.GiftCardId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class GiftCardTransactionConfiguration : IEntityTypeConfiguration<GiftCardTransaction>
{
    public void Configure(EntityTypeBuilder<GiftCardTransaction> builder)
    {
        builder.ToTable("GiftCardTransaction");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountDelta).HasPrecision(18, 4);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(GiftCard.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(GiftCard.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(GiftCardTransaction.ReasonMaxLength);
        builder.HasIndex(x => new { x.GiftCardId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => x.GiftCardId);
    }
}
