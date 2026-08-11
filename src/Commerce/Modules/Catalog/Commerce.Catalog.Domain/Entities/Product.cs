using Commerce.Catalog.Domain.Enums;
using Commerce.Catalog.Domain.Events;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class Product : AggregateRoot
{
    public const int NameMaxLength = 400;
    public const int ShortDescriptionMaxLength = 1000;

    private Product()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? ShortDescription { get; private set; }

    public string? Description { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public ProductType ProductType { get; private set; }

    public bool Published { get; private set; }

    public bool IsVisible { get; private set; }

    public bool IsAvailable { get; private set; }

    public bool Deleted { get; private set; }

    public int DisplayOrder { get; private set; }

    public string? Slug { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Product Create(
        string name,
        Sku sku,
        ProductType productType,
        string? shortDescription = null,
        string? description = null,
        Slug? slug = null,
        bool published = false,
        bool isVisible = true,
        bool isAvailable = true,
        int displayOrder = 0)
    {
        var product = new Product
        {
            Name = NormalizeName(name),
            Sku = sku.Value,
            ProductType = productType,
            ShortDescription = NormalizeOptional(shortDescription, ShortDescriptionMaxLength),
            Description = description?.Trim(),
            Slug = slug?.Value,
            Published = published,
            IsVisible = isVisible,
            IsAvailable = isAvailable,
            DisplayOrder = displayOrder,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, product.Sku, product.Name));
        return product;
    }

    public void UpdateDetails(
        string name,
        ProductType productType,
        string? shortDescription,
        string? description,
        Slug? slug,
        bool published,
        bool isVisible,
        bool isAvailable,
        int displayOrder)
    {
        EnsureNotDeleted();

        Name = NormalizeName(name);
        ProductType = productType;
        ShortDescription = NormalizeOptional(shortDescription, ShortDescriptionMaxLength);
        Description = description?.Trim();
        Slug = slug?.Value;
        Published = published;
        IsVisible = isVisible;
        IsAvailable = isAvailable;
        DisplayOrder = displayOrder;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ProductUpdatedEvent(Id, Sku, Name));
    }

    public void SoftDelete()
    {
        if (Deleted)
        {
            return;
        }

        Deleted = true;
        Published = false;
        IsVisible = false;
        IsAvailable = false;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ProductDeletedEvent(Id, Sku, Name));
    }

    public bool IsPubliclyVisible() => Published && IsVisible && IsAvailable && !Deleted;

    private void EnsureNotDeleted()
    {
        if (Deleted)
        {
            throw new InvalidOperationException("Deleted products cannot be modified.");
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Product name cannot exceed {NameMaxLength} characters.", nameof(name));
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", nameof(value));
        }

        return trimmed;
    }
}
