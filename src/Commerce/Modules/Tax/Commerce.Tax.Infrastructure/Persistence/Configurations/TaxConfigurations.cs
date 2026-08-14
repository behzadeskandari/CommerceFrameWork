using Commerce.Tax.Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace Commerce.Tax.Infrastructure.Persistence.Configurations;



internal sealed class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>

{

    public void Configure(EntityTypeBuilder<TaxCategory> builder)

    {

        builder.ToTable("TaxCategory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(TaxCategory.NameMaxLength).IsRequired();

        builder.Property(x => x.SystemName).HasMaxLength(TaxCategory.SystemNameMaxLength).IsRequired();

        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();

        builder.HasIndex(x => x.StoreId);

        builder.HasIndex(x => x.IsActive);

    }

}



internal sealed class TaxZoneConfiguration : IEntityTypeConfiguration<TaxZone>

{

    public void Configure(EntityTypeBuilder<TaxZone> builder)

    {

        builder.ToTable("TaxZone");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(TaxZone.NameMaxLength).IsRequired();

        builder.Property(x => x.SystemName).HasMaxLength(TaxZone.SystemNameMaxLength).IsRequired();

        builder.HasIndex(x => new { x.StoreId, x.SystemName }).IsUnique();

        builder.HasIndex(x => x.StoreId);



        builder.HasMany(x => x.Countries).WithOne().HasForeignKey(x => x.TaxZoneId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Countries).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.States).WithOne().HasForeignKey(x => x.TaxZoneId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.States).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.PostalRules).WithOne().HasForeignKey(x => x.TaxZoneId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.PostalRules).UsePropertyAccessMode(PropertyAccessMode.Field);

    }

}



internal sealed class TaxZoneCountryConfiguration : IEntityTypeConfiguration<TaxZoneCountry>

{

    public void Configure(EntityTypeBuilder<TaxZoneCountry> builder)

    {

        builder.ToTable("TaxZoneCountry");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();

        builder.HasIndex(x => new { x.TaxZoneId, x.CountryCode });

    }

}



internal sealed class TaxZoneStateConfiguration : IEntityTypeConfiguration<TaxZoneState>

{

    public void Configure(EntityTypeBuilder<TaxZoneState> builder)

    {

        builder.ToTable("TaxZoneState");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();

        builder.Property(x => x.StateProvince).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.TaxZoneId, x.CountryCode, x.StateProvince });

    }

}



internal sealed class TaxZonePostalRuleConfiguration : IEntityTypeConfiguration<TaxZonePostalRule>

{

    public void Configure(EntityTypeBuilder<TaxZonePostalRule> builder)

    {

        builder.ToTable("TaxZonePostalRule");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();

        builder.Property(x => x.PostalFrom).HasMaxLength(32).IsRequired();

        builder.Property(x => x.PostalTo).HasMaxLength(32);

        builder.HasIndex(x => new { x.TaxZoneId, x.CountryCode });

    }

}



internal sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>

{

    public void Configure(EntityTypeBuilder<TaxRate> builder)

    {

        builder.ToTable("TaxRate");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Percentage).HasPrecision(18, 4);

        builder.Property(x => x.FixedAmount).HasPrecision(18, 4);

        builder.HasIndex(x => x.StoreId);

        builder.HasIndex(x => x.TaxCategoryId);

        builder.HasIndex(x => x.TaxZoneId);

    }

}


