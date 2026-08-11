using Commerce.Catalog.Contracts.Categories;
using Commerce.Catalog.Contracts.Products;

namespace Commerce.Catalog.Contracts.Catalog;

public interface IProductCatalog
{
    Task<ProductDetailDto?> GetProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductSummaryDto>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<CategoryDetailDto?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
