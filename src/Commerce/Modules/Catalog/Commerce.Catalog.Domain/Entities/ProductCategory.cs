using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductCategory : Entity
{
    private ProductCategory()
    {
    }

    public int ProductId { get; private set; }

    public int CategoryId { get; private set; }

    public int DisplayOrder { get; private set; }

    public static ProductCategory Create(int productId, int categoryId, int displayOrder = 0) =>
        new()
        {
            ProductId = productId,
            CategoryId = categoryId,
            DisplayOrder = displayOrder
        };
}
