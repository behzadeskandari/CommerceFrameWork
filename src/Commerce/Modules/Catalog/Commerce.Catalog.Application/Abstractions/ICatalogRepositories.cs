using Commerce.Catalog.Domain.Entities;

namespace Commerce.Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(bool includeDeleted, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task DeleteAsync(Category category, CancellationToken cancellationToken = default);
}

public interface IProductCategoryRepository
{
    Task<bool> ExistsAsync(int productId, int categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(ProductCategory relationship, CancellationToken cancellationToken = default);

    Task RemoveAsync(int productId, int categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetCategoryIdsForProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetProductIdsForCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<bool> CategoryHasProductsAsync(int categoryId, CancellationToken cancellationToken = default);
}

public interface IProductAttributeRepository
{
    Task<ProductAttributeDefinition?> GetDefinitionByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task AddDefinitionAsync(ProductAttributeDefinition definition, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAttributeValue>> GetValuesForProductAsync(int productId, CancellationToken cancellationToken = default);

    Task AddOrUpdateValueAsync(ProductAttributeValue value, CancellationToken cancellationToken = default);
}
