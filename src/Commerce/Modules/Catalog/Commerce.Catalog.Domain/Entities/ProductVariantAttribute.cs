using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductVariantAttribute : Entity
{
    private ProductVariantAttribute()
    {
    }

    public int VariantId { get; private set; }

    public int AttributeOptionId { get; private set; }

    public static ProductVariantAttribute Create(int variantId, int attributeOptionId)
    {
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(variantId));
        }

        if (attributeOptionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attributeOptionId));
        }

        return new ProductVariantAttribute
        {
            VariantId = variantId,
            AttributeOptionId = attributeOptionId
        };
    }
}
