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

    public int DisplayOrder { get; private set; }

    public static ProductAttributeDefinition Create(string name, string code, int displayOrder = 0)
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

        return new ProductAttributeDefinition
        {
            Name = trimmedName,
            Code = normalizedCode,
            DisplayOrder = displayOrder
        };
    }
}
