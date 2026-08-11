using Commerce.Catalog.Domain.Events;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductVariant : AggregateRoot
{
    public const int NameMaxLength = 400;

    private readonly List<ProductVariantAttribute> _attributes = [];
    private IReadOnlyList<int> _pendingOptionIds = [];

    private ProductVariant()
    {
    }

    public int ProductId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsDefault { get; private set; }

    public int DisplayOrder { get; private set; }

    public string AttributeCombinationKey { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ProductVariantAttribute> Attributes => _attributes.AsReadOnly();

    internal IReadOnlyList<int> PendingAttributeOptionIds => _pendingOptionIds;

    public static ProductVariant Create(
        int productId,
        Sku sku,
        string name,
        IEnumerable<int> attributeOptionIds,
        bool isActive = true,
        bool isDefault = false,
        int displayOrder = 0)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variant name is required.", nameof(name));
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku = sku.Value,
            Name = name.Trim(),
            IsActive = isActive,
            IsDefault = isDefault,
            DisplayOrder = displayOrder,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        variant.ApplyAttributeOptions(attributeOptionIds);
        variant.RaiseDomainEvent(new VariantCreatedEvent(variant.Id, variant.ProductId, variant.Sku));
        return variant;
    }

    public void UpdateDetails(
        string name,
        bool isActive,
        bool isDefault,
        int displayOrder,
        IEnumerable<int> attributeOptionIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variant name is required.", nameof(name));
        }

        Name = name.Trim();
        IsActive = isActive;
        IsDefault = isDefault;
        DisplayOrder = displayOrder;
        ApplyAttributeOptions(attributeOptionIds);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new VariantUpdatedEvent(Id, ProductId, Sku));
    }

    internal void AttachAttribute(ProductVariantAttribute attribute) => _attributes.Add(attribute);

    public void MaterializeAttributes()
    {
        if (Id <= 0)
        {
            throw new InvalidOperationException("Variant must be persisted before materializing attributes.");
        }

        _attributes.Clear();
        foreach (var optionId in _pendingOptionIds)
        {
            _attributes.Add(ProductVariantAttribute.Create(Id, optionId));
        }
    }

    private void ApplyAttributeOptions(IEnumerable<int> attributeOptionIds)
    {
        _pendingOptionIds = attributeOptionIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        AttributeCombinationKey = BuildCombinationKey(_pendingOptionIds);
    }

    public static string BuildCombinationKey(IReadOnlyList<int> sortedOptionIds) =>
        sortedOptionIds.Count == 0 ? string.Empty : string.Join(':', sortedOptionIds);
}
