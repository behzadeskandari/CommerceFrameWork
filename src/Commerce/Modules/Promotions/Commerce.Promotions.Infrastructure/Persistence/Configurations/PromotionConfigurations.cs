using Commerce.Promotions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Promotions.Infrastructure.Persistence.Configurations;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Promotion.NameMaxLength).IsRequired();
        builder.Property(x => x.SystemName).HasMaxLength(Promotion.SystemNameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Promotion.DescriptionMaxLength);
        builder.Property(x => x.CombinationGroup).HasMaxLength(Promotion.CombinationGroupMaxLength);
        builder.Property(x => x.CouponCode).HasMaxLength(Promotion.CouponCodeMaxLength);
        builder.HasIndex(x => x.SystemName).IsUnique();
        builder.HasIndex(x => new { x.StoreId, x.IsActive });
        builder.HasMany<PromotionCondition>("_conditions")
            .WithOne()
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany<PromotionAction>("_actions")
            .WithOne()
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_conditions").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation("_actions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PromotionConditionConfiguration : IEntityTypeConfiguration<PromotionCondition>
{
    public void Configure(EntityTypeBuilder<PromotionCondition> builder)
    {
        builder.ToTable("PromotionConditions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParametersJson).HasMaxLength(PromotionCondition.ParametersMaxLength).IsRequired();
    }
}

public sealed class PromotionActionConfiguration : IEntityTypeConfiguration<PromotionAction>
{
    public void Configure(EntityTypeBuilder<PromotionAction> builder)
    {
        builder.ToTable("PromotionActions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParametersJson).HasMaxLength(PromotionAction.ParametersMaxLength).IsRequired();
    }
}

public sealed class PromotionUsageConfiguration : IEntityTypeConfiguration<PromotionUsage>
{
    public void Configure(EntityTypeBuilder<PromotionUsage> builder)
    {
        builder.ToTable("PromotionUsages");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PromotionId, x.CustomerId });
    }
}
