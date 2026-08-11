using Commerce.Catalog.Domain.Events;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class Category : AggregateRoot
{
    public const int NameMaxLength = 400;

    private Category()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int? ParentCategoryId { get; private set; }

    public bool Published { get; private set; }

    public int DisplayOrder { get; private set; }

    public string? Slug { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Category Create(
        string name,
        int? parentCategoryId = null,
        string? description = null,
        Slug? slug = null,
        bool published = false,
        int displayOrder = 0)
    {
        var category = new Category
        {
            Name = NormalizeName(name),
            ParentCategoryId = parentCategoryId,
            Description = description?.Trim(),
            Slug = slug?.Value,
            Published = published,
            DisplayOrder = displayOrder,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        category.RaiseDomainEvent(new CategoryCreatedEvent(category.Id, category.Name));
        return category;
    }

    public void UpdateDetails(
        string name,
        int? parentCategoryId,
        string? description,
        Slug? slug,
        bool published,
        int displayOrder)
    {
        Name = NormalizeName(name);
        ParentCategoryId = parentCategoryId;
        Description = description?.Trim();
        Slug = slug?.Value;
        Published = published;
        DisplayOrder = displayOrder;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CategoryUpdatedEvent(Id, Name));
    }

    public void MarkDeleted()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CategoryDeletedEvent(Id, Name));
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Category name cannot exceed {NameMaxLength} characters.", nameof(name));
        }

        return trimmed;
    }
}
