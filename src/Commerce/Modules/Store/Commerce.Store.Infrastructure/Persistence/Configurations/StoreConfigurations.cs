using Commerce.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Store.Infrastructure.Persistence.Configurations;

internal sealed class StoreStoreConfiguration : IEntityTypeConfiguration<StoreEntity>
{
    public void Configure(EntityTypeBuilder<StoreEntity> builder)
    {
        builder.ToTable("StoreStore");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemName).HasMaxLength(StoreEntity.SystemNameMaxLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(StoreEntity.NameMaxLength).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(StoreEntity.UrlMaxLength).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.SystemName).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.DisplayOrder);
        builder.HasMany(x => x.Domains)
            .WithOne()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Domains).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StoreStoreDomainConfiguration : IEntityTypeConfiguration<StoreDomain>
{
    public void Configure(EntityTypeBuilder<StoreDomain> builder)
    {
        builder.ToTable("StoreStoreDomain");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Host).HasMaxLength(StoreDomain.HostMaxLength).IsRequired();
        builder.Property(x => x.Scheme).HasMaxLength(StoreDomain.SchemeMaxLength).IsRequired();
        builder.HasIndex(x => new { x.Host, x.Port }).IsUnique();
        builder.HasIndex(x => x.StoreId);
    }
}

internal sealed class StoreLanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("StoreLanguage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Language.NameMaxLength).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(Language.CodeMaxLength).IsRequired();
        builder.Property(x => x.CultureCode).HasMaxLength(Language.CultureMaxLength).IsRequired();
        builder.Property(x => x.NativeName).HasMaxLength(Language.NameMaxLength).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.LanguageCode).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

internal sealed class StoreCurrencyConfiguration : IEntityTypeConfiguration<StoreCurrency>
{
    public void Configure(EntityTypeBuilder<StoreCurrency> builder)
    {
        builder.ToTable("StoreCurrency");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(StoreCurrency.CodeMaxLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(StoreCurrency.NameMaxLength).IsRequired();
        builder.Property(x => x.Symbol).HasMaxLength(StoreCurrency.SymbolMaxLength).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(StoreCurrency.NameMaxLength).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(18, 4);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

internal sealed class StoreEntityTranslationConfiguration : IEntityTypeConfiguration<EntityTranslation>
{
    public void Configure(EntityTypeBuilder<EntityTranslation> builder)
    {
        builder.ToTable("StoreEntityTranslation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(EntityTranslation.EntityTypeMaxLength).IsRequired();
        builder.Property(x => x.Property).HasMaxLength(EntityTranslation.PropertyMaxLength).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(EntityTranslation.ValueMaxLength).IsRequired();
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.LanguageId, x.Property }).IsUnique();
        builder.HasIndex(x => x.LanguageId);
    }
}

internal sealed class StoreMediaConfiguration : IEntityTypeConfiguration<StoreMedia>
{
    public void Configure(EntityTypeBuilder<StoreMedia> builder)
    {
        builder.ToTable("StoreMedia");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.StoreId, x.Role }).IsUnique();
        builder.HasIndex(x => x.MediaAssetId);
        builder.HasOne<StoreEntity>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
