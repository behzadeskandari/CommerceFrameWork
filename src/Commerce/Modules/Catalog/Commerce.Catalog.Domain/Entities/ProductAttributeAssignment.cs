using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductAttributeAssignment : Entity
{
    private ProductAttributeAssignment()
    {
    }

    public int ProductId { get; private set; }

    public int AttributeDefinitionId { get; private set; }

    public int DisplayOrder { get; private set; }

    public static ProductAttributeAssignment Create(int productId, int attributeDefinitionId, int displayOrder = 0)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        if (attributeDefinitionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attributeDefinitionId));
        }

        return new ProductAttributeAssignment
        {
            ProductId = productId,
            AttributeDefinitionId = attributeDefinitionId,
            DisplayOrder = displayOrder
        };
    }
}
