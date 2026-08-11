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
        builder.HasIndex(x => x.Code).IsUnique();
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
