using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductAttributeValue : Entity
{
    public const int ValueMaxLength = 1000;

    private ProductAttributeValue()
    {
    }

    public int ProductId { get; private set; }

    public int AttributeDefinitionId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public static ProductAttributeValue Create(int productId, int attributeDefinitionId, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Attribute value is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ValueMaxLength)
        {
            throw new ArgumentException($"Attribute value cannot exceed {ValueMaxLength} characters.", nameof(value));
        }

        return new ProductAttributeValue
        {
            ProductId = productId,
            AttributeDefinitionId = attributeDefinitionId,
            Value = trimmed
        };
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Attribute value is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ValueMaxLength)
        {
            throw new ArgumentException($"Attribute value cannot exceed {ValueMaxLength} characters.", nameof(value));
        }

        Value = trimmed;
    }
}
