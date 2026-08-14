using Commerce.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Customers.Infrastructure.Persistence.Configurations;

internal sealed class CustomerPreferenceConfiguration : IEntityTypeConfiguration<CustomerPreference>
{
    public void Configure(EntityTypeBuilder<CustomerPreference> builder)
    {
        builder.ToTable("CustomerPreference");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreferenceKey).HasMaxLength(CustomerPreference.KeyMaxLength).IsRequired();
        builder.Property(x => x.PreferenceValue).HasMaxLength(CustomerPreference.ValueMaxLength).IsRequired();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.CustomerId, x.StoreId, x.PreferenceKey }).IsUnique();
    }
}

internal sealed class CustomerSegmentConfiguration : IEntityTypeConfiguration<CustomerSegment>
{
    public void Configure(EntityTypeBuilder<CustomerSegment> builder)
    {
        builder.ToTable("CustomerSegment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(CustomerSegment.NameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(CustomerSegment.DescriptionMaxLength);
        builder.HasIndex(x => x.StoreId);
        builder.HasMany(x => x.Rules).WithOne().HasForeignKey(x => x.CustomerSegmentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CustomerSegmentRuleConfiguration : IEntityTypeConfiguration<CustomerSegmentRule>
{
    public void Configure(EntityTypeBuilder<CustomerSegmentRule> builder)
    {
        builder.ToTable("CustomerSegmentRule");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MinLifetimeSpend).HasPrecision(18, 4);
        builder.HasIndex(x => x.CustomerSegmentId);
    }
}

internal sealed class CustomerSegmentMembershipConfiguration : IEntityTypeConfiguration<CustomerSegmentMembership>
{
    public void Configure(EntityTypeBuilder<CustomerSegmentMembership> builder)
    {
        builder.ToTable("CustomerSegmentMembership");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CustomerSegmentId, x.CustomerId, x.StoreId }).IsUnique();
        builder.HasIndex(x => x.CustomerId);
    }
}

internal sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("LoyaltyAccount");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CustomerId, x.StoreId }).IsUnique();
        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.LoyaltyAccountId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("LoyaltyTransaction");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(LoyaltyAccount.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(LoyaltyTransaction.ReasonMaxLength);
        builder.HasIndex(x => x.LoyaltyAccountId);
        builder.HasIndex(x => new { x.LoyaltyAccountId, x.IdempotencyKey }).IsUnique();
    }
}

internal sealed class LoyaltyRewardConfiguration : IEntityTypeConfiguration<LoyaltyReward>
{
    public void Configure(EntityTypeBuilder<LoyaltyReward> builder)
    {
        builder.ToTable("LoyaltyReward");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(LoyaltyReward.NameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(LoyaltyReward.DescriptionMaxLength);
        builder.HasIndex(x => x.StoreId);
    }
}

internal sealed class LoyaltyRewardRedemptionConfiguration : IEntityTypeConfiguration<LoyaltyRewardRedemption>
{
    public void Configure(EntityTypeBuilder<LoyaltyRewardRedemption> builder)
    {
        builder.ToTable("LoyaltyRewardRedemption");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(LoyaltyRewardRedemption.IdempotencyKeyMaxLength).IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.StoreId, x.IdempotencyKey }).IsUnique();
    }
}

internal sealed class StoreCreditAccountConfiguration : IEntityTypeConfiguration<StoreCreditAccount>
{
    public void Configure(EntityTypeBuilder<StoreCreditAccount> builder)
    {
        builder.ToTable("StoreCreditAccount");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(StoreCreditAccount.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.Balance).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.CustomerId, x.StoreId, x.CurrencyCode }).IsUnique();
        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.StoreCreditAccountId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StoreCreditTransactionConfiguration : IEntityTypeConfiguration<StoreCreditTransaction>
{
    public void Configure(EntityTypeBuilder<StoreCreditTransaction> builder)
    {
        builder.ToTable("StoreCreditTransaction");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountDelta).HasPrecision(18, 4);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(StoreCreditAccount.CurrencyCodeMaxLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(StoreCreditAccount.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(StoreCreditTransaction.ReasonMaxLength);
        builder.HasIndex(x => x.StoreCreditAccountId);
        builder.HasIndex(x => new { x.StoreCreditAccountId, x.IdempotencyKey }).IsUnique();
    }
}

internal sealed class CustomerActivityLogConfiguration : IEntityTypeConfiguration<CustomerActivityLog>
{
    public void Configure(EntityTypeBuilder<CustomerActivityLog> builder)
    {
        builder.ToTable("CustomerActivityLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Summary).HasMaxLength(CustomerActivityLog.SummaryMaxLength).IsRequired();
        builder.Property(x => x.DetailsJson).HasMaxLength(CustomerActivityLog.DetailsMaxLength);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc });
    }
}
