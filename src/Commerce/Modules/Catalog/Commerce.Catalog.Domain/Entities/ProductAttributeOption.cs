using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductAttributeOption : Entity
{
    public const int ValueMaxLength = 200;

    private ProductAttributeOption()
    {
    }

    public int AttributeDefinitionId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static ProductAttributeOption Create(
        int attributeDefinitionId,
        string value,
        int displayOrder = 0,
        bool isActive = true)
    {
        if (attributeDefinitionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attributeDefinitionId));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Option value is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ValueMaxLength)
        {
            throw new ArgumentException($"Option value cannot exceed {ValueMaxLength} characters.", nameof(value));
        }

        var now = DateTime.UtcNow;
        return new ProductAttributeOption
        {
            AttributeDefinitionId = attributeDefinitionId,
            Value = trimmed,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string value, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Option value is required.", nameof(value));
        }

        Value = value.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
