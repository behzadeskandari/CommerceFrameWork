using Commerce.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class CatalogProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("CatalogProduct");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(Product.ShortDescriptionMaxLength);
        builder.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200);
        builder.Property(x => x.ProductType).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.Published);
        builder.HasIndex(x => x.Deleted);
        builder.HasIndex(x => x.IsVisible);
        builder.HasIndex(x => x.IsAvailable);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
    }
}

internal sealed class CatalogCategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("CatalogCategory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.ParentCategoryId);
        builder.HasIndex(x => x.Published);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CatalogProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("CatalogProductCategory");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProductId, x.CategoryId }).IsUnique();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogProductAttributeDefinitionConfiguration : IEntityTypeConfiguration<ProductAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<ProductAttributeDefinition> builder)
    {
        builder.ToTable("CatalogProductAttribute");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(ProductAttributeDefinition.NameMaxLength).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(ProductAttributeDefinition.CodeMaxLength).IsRequired();
        builder.Property(x => x.AttributeType).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

internal sealed class CatalogProductAttributeOptionConfiguration : IEntityTypeConfiguration<ProductAttributeOption>
{
    public void Configure(EntityTypeBuilder<ProductAttributeOption> builder)
    {
        builder.ToTable("CatalogProductAttributeOption");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasMaxLength(ProductAttributeOption.ValueMaxLength).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.AttributeDefinitionId, x.Value }).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasOne<ProductAttributeDefinition>()
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogProductAttributeAssignmentConfiguration : IEntityTypeConfiguration<ProductAttributeAssignment>
{
    public void Configure(EntityTypeBuilder<ProductAttributeAssignment> builder)
    {
        builder.ToTable("CatalogProductAttributeAssignment");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProductId, x.AttributeDefinitionId }).IsUnique();
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductAttributeDefinition>()
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("CatalogProductAttributeValue");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasMaxLength(ProductAttributeValue.ValueMaxLength).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.AttributeDefinitionId }).IsUnique();
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductAttributeDefinition>()
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("CatalogProductVariant");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(ProductVariant.NameMaxLength).IsRequired();
        builder.Property(x => x.AttributeCombinationKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.ProductId, x.AttributeCombinationKey }).IsUnique();
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Attributes).HasField("_attributes");
    }
}

internal sealed class CatalogProductVariantAttributeConfiguration : IEntityTypeConfiguration<ProductVariantAttribute>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
    {
        builder.ToTable("CatalogProductVariantAttribute");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.VariantId, x.AttributeOptionId }).IsUnique();
        builder.HasOne<ProductVariant>()
            .WithMany(x => x.Attributes)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductAttributeOption>()
            .WithMany()
            .HasForeignKey(x => x.AttributeOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CatalogProductOfferConfiguration : IEntityTypeConfiguration<ProductOffer>
{
    public void Configure(EntityTypeBuilder<ProductOffer> builder)
    {
        builder.ToTable("CatalogProductOffer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 4);
        builder.Property(x => x.CompareAtPrice).HasPrecision(18, 4);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.VariantId);
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CurrencyId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.StoreId, x.ProductId, x.VariantId, x.CurrencyId });
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.ToTable("CatalogProductMedia");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.ProductId, x.MediaAssetId }).IsUnique();
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogProductVariantMediaConfiguration : IEntityTypeConfiguration<ProductVariantMedia>
{
    public void Configure(EntityTypeBuilder<ProductVariantMedia> builder)
    {
        builder.ToTable("CatalogProductVariantMedia");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.VariantId);
        builder.HasIndex(x => new { x.VariantId, x.MediaAssetId }).IsUnique();
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogCategoryMediaConfiguration : IEntityTypeConfiguration<CategoryMedia>
{
    public void Configure(EntityTypeBuilder<CategoryMedia> builder)
    {
        builder.ToTable("CatalogCategoryMedia");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CategoryId).IsUnique();
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
