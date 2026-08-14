using Commerce.Catalog.Domain.Entities;

namespace Commerce.Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(bool includeDeleted, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> SearchAsync(
        string? term,
        bool publicOnly,
        CancellationToken cancellationToken = default);

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
    Task<ProductAttributeDefinition?> GetDefinitionByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductAttributeDefinition?> GetDefinitionByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAttributeDefinition>> ListDefinitionsAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task AddDefinitionAsync(ProductAttributeDefinition definition, CancellationToken cancellationToken = default);

    Task UpdateDefinitionAsync(ProductAttributeDefinition definition, CancellationToken cancellationToken = default);

    Task<ProductAttributeOption?> GetOptionByIdAsync(int optionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAttributeOption>> GetOptionsByIdsAsync(
        IReadOnlyCollection<int> optionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAttributeOption>> GetOptionsForDefinitionAsync(
        int attributeDefinitionId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task AddOptionAsync(ProductAttributeOption option, CancellationToken cancellationToken = default);

    Task UpdateOptionAsync(ProductAttributeOption option, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAttributeAssignment>> GetAssignmentsForProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task AddAssignmentAsync(ProductAttributeAssignment assignment, CancellationToken cancellationToken = default);

    Task RemoveAssignmentAsync(int productId, int attributeDefinitionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAttributeValue>> GetValuesForProductAsync(int productId, CancellationToken cancellationToken = default);

    Task AddOrUpdateValueAsync(ProductAttributeValue value, CancellationToken cancellationToken = default);
}

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(int variantId, CancellationToken cancellationToken = default);

    Task<ProductVariant?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>> ListForProductAsync(
        int productId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<bool> CombinationExistsAsync(int productId, string combinationKey, int? excludeVariantId, CancellationToken cancellationToken = default);

    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductVariant variant, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProductVariant variant, CancellationToken cancellationToken = default);
}

public interface IProductOfferRepository
{
    Task<ProductOffer?> GetByIdAsync(int offerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductOffer>> ListForProductAsync(
        int productId,
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductOffer>> ListForVariantAsync(
        int variantId,
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<ProductOffer?> FindActiveOfferAsync(
        int productId,
        int? variantId,
        int storeId,
        int currencyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductOffer offer, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductOffer offer, CancellationToken cancellationToken = default);
}

public interface IOfferTierPriceRepository
{
    Task<IReadOnlyList<OfferTierPrice>> ListForOfferAsync(int offerId, CancellationToken cancellationToken = default);

    Task<OfferTierPrice?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<decimal?> ResolveTierUnitPriceAsync(int offerId, int quantity, CancellationToken cancellationToken = default);

    Task AddAsync(OfferTierPrice tierPrice, CancellationToken cancellationToken = default);

    Task UpdateAsync(OfferTierPrice tierPrice, CancellationToken cancellationToken = default);

    Task DeleteAsync(OfferTierPrice tierPrice, CancellationToken cancellationToken = default);
}

public interface IProductMediaRepository
{
    Task<IReadOnlyList<ProductMedia>> ListForProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductMedia>> ListForProductsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default);

    Task<ProductMedia?> GetAsync(int productId, int mediaAssetId, CancellationToken cancellationToken = default);

    Task AddAsync(ProductMedia media, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductMedia media, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProductMedia media, CancellationToken cancellationToken = default);
}

public interface IProductVariantMediaRepository
{
    Task<IReadOnlyList<ProductVariantMedia>> ListForVariantAsync(int variantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariantMedia>> ListForVariantsAsync(IReadOnlyCollection<int> variantIds, CancellationToken cancellationToken = default);

    Task<ProductVariantMedia?> GetAsync(int variantId, int mediaAssetId, CancellationToken cancellationToken = default);

    Task AddAsync(ProductVariantMedia media, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProductVariantMedia media, CancellationToken cancellationToken = default);
}

public interface ICategoryMediaRepository
{
    Task<CategoryMedia?> GetForCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(CategoryMedia media, CancellationToken cancellationToken = default);

    Task DeleteAsync(CategoryMedia media, CancellationToken cancellationToken = default);
}
