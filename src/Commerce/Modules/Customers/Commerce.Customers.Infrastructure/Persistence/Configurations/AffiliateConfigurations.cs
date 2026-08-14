using Commerce.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Customers.Infrastructure.Persistence.Configurations;

internal sealed class AffiliateConfiguration : IEntityTypeConfiguration<Affiliate>
{
    public void Configure(EntityTypeBuilder<Affiliate> builder)
    {
        builder.ToTable("Affiliate");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReferralCode).HasMaxLength(Affiliate.ReferralCodeMaxLength).IsRequired();
        builder.Property(x => x.CommissionRatePercent).HasPrecision(5, 2);
        builder.HasIndex(x => new { x.StoreId, x.ReferralCode }).IsUnique();
        builder.HasIndex(x => new { x.StoreId, x.CustomerId }).IsUnique();
        builder.HasIndex(x => x.StoreId);
    }
}

internal sealed class AffiliateReferralConfiguration : IEntityTypeConfiguration<AffiliateReferral>
{
    public void Configure(EntityTypeBuilder<AffiliateReferral> builder)
    {
        builder.ToTable("AffiliateReferral");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.AffiliateId, x.ReferredCustomerId }).IsUnique();
        builder.HasIndex(x => x.ReferredCustomerId);
    }
}

internal sealed class AffiliateCommissionAccountConfiguration : IEntityTypeConfiguration<AffiliateCommissionAccount>
{
    public void Configure(EntityTypeBuilder<AffiliateCommissionAccount> builder)
    {
        builder.ToTable("AffiliateCommissionAccount");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(AffiliateCommissionAccount.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.Balance).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.AffiliateId, x.CurrencyCode }).IsUnique();
        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.AffiliateCommissionAccountId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AffiliateCommissionTransactionConfiguration : IEntityTypeConfiguration<AffiliateCommissionTransaction>
{
    public void Configure(EntityTypeBuilder<AffiliateCommissionTransaction> builder)
    {
        builder.ToTable("AffiliateCommissionTransaction");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountDelta).HasPrecision(18, 4);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(AffiliateCommissionAccount.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(AffiliateCommissionAccount.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(AffiliateCommissionTransaction.ReasonMaxLength);
        builder.HasIndex(x => new { x.AffiliateCommissionAccountId, x.IdempotencyKey }).IsUnique();
    }
}
