using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductAttributeDefinition : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 100;

    private ProductAttributeDefinition()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public AttributeType AttributeType { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static ProductAttributeDefinition Create(
        string name,
        string code,
        AttributeType attributeType,
        int displayOrder = 0,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Attribute code is required.", nameof(code));
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        if (normalizedCode.Length > CodeMaxLength)
        {
            throw new ArgumentException($"Attribute code cannot exceed {CodeMaxLength} characters.", nameof(code));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentException($"Attribute name cannot exceed {NameMaxLength} characters.", nameof(name));
        }

        var now = DateTime.UtcNow;
        return new ProductAttributeDefinition
        {
            Name = trimmedName,
            Code = normalizedCode,
            AttributeType = attributeType,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string name, AttributeType attributeType, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name is required.", nameof(name));
        }

        Name = name.Trim();
        AttributeType = attributeType;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
