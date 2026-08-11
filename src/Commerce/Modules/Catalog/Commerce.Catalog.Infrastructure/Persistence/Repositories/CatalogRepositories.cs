using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class EfProductRepository(CommerceDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Product>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        dbContext.Set<Product>().FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Set<Product>().FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Product>().AsQueryable();
        if (!includeDeleted)
        {
            query = query.Where(x => !x.Deleted);
        }

        return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string? term,
        bool publicOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Product>().AsQueryable();

        if (!publicOnly)
        {
            query = query.Where(x => !x.Deleted);
        }
        else
        {
            query = query.Where(x => x.Published && x.IsVisible && x.IsAvailable && !x.Deleted);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var normalized = term.Trim();
            query = query.Where(x =>
                x.Name.Contains(normalized) ||
                x.Sku.Contains(normalized) ||
                (x.Slug != null && x.Slug.Contains(normalized)));
        }

        return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Product>().Add(product);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Product>().Update(product);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfCategoryRepository(CommerceDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Category>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<Category>()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Category>().AnyAsync(x => x.ParentCategoryId == categoryId, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Category>().Add(category);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Category>().Update(category);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Category>().Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfProductCategoryRepository(CommerceDbContext dbContext) : IProductCategoryRepository
{
    public Task<bool> ExistsAsync(int productId, int categoryId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductCategory>().AnyAsync(x => x.ProductId == productId && x.CategoryId == categoryId, cancellationToken);

    public async Task AddAsync(ProductCategory relationship, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductCategory>().Add(relationship);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(int productId, int categoryId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Set<ProductCategory>()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.CategoryId == categoryId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            dbContext.Set<ProductCategory>().Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<int>> GetCategoryIdsForProductAsync(int productId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ProductCategory>()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<int>> GetProductIdsForCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ProductCategory>()
            .Where(x => x.CategoryId == categoryId)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> CategoryHasProductsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductCategory>().AnyAsync(x => x.CategoryId == categoryId, cancellationToken);
}

internal sealed class EfProductAttributeRepository(CommerceDbContext dbContext) : IProductAttributeRepository
{
    public Task<ProductAttributeDefinition?> GetDefinitionByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductAttributeDefinition>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<ProductAttributeDefinition?> GetDefinitionByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductAttributeDefinition>()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ProductAttributeDefinition>> ListDefinitionsAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductAttributeDefinition>().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddDefinitionAsync(ProductAttributeDefinition definition, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductAttributeDefinition>().Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateDefinitionAsync(ProductAttributeDefinition definition, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductAttributeDefinition>().Update(definition);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<ProductAttributeOption?> GetOptionByIdAsync(int optionId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductAttributeOption>().FirstOrDefaultAsync(x => x.Id == optionId, cancellationToken);

    public async Task<IReadOnlyList<ProductAttributeOption>> GetOptionsForDefinitionAsync(
        int attributeDefinitionId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductAttributeOption>()
            .Where(x => x.AttributeDefinitionId == attributeDefinitionId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Value).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddOptionAsync(ProductAttributeOption option, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductAttributeOption>().Add(option);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateOptionAsync(ProductAttributeOption option, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductAttributeOption>().Update(option);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProductAttributeAssignment>> GetAssignmentsForProductAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<ProductAttributeAssignment>()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAssignmentAsync(ProductAttributeAssignment assignment, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductAttributeAssignment>().Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAssignmentAsync(int productId, int attributeDefinitionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Set<ProductAttributeAssignment>()
            .FirstOrDefaultAsync(
                x => x.ProductId == productId && x.AttributeDefinitionId == attributeDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            dbContext.Set<ProductAttributeAssignment>().Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ProductAttributeValue>> GetValuesForProductAsync(int productId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ProductAttributeValue>()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddOrUpdateValueAsync(ProductAttributeValue value, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<ProductAttributeValue>()
            .FirstOrDefaultAsync(
                x => x.ProductId == value.ProductId && x.AttributeDefinitionId == value.AttributeDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.Set<ProductAttributeValue>().Add(value);
        }
        else
        {
            existing.UpdateValue(value.Value);
            dbContext.Set<ProductAttributeValue>().Update(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

// Product and variant SKUs share a single global unique index (see CatalogConfigurations).
internal sealed class EfProductVariantRepository(CommerceDbContext dbContext) : IProductVariantRepository
{
    public Task<ProductVariant?> GetByIdAsync(int variantId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductVariant>()
            .Include(x => x.Attributes)
            .FirstOrDefaultAsync(x => x.Id == variantId, cancellationToken);

    public Task<ProductVariant?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductVariant>().FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<ProductVariant>> ListForProductAsync(
        int productId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductVariant>()
            .Include(x => x.Attributes)
            .Where(x => x.ProductId == productId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> CombinationExistsAsync(
        int productId,
        string combinationKey,
        int? excludeVariantId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductVariant>()
            .Where(x => x.ProductId == productId && x.AttributeCombinationKey == combinationKey);

        if (excludeVariantId.HasValue)
        {
            query = query.Where(x => x.Id != excludeVariantId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductVariant>().Add(variant);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        variant.MaterializeAttributes();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductVariant>().Update(variant);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        variant.MaterializeAttributes();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductVariant>().Remove(variant);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfProductOfferRepository(CommerceDbContext dbContext) : IProductOfferRepository
{
    public Task<ProductOffer?> GetByIdAsync(int offerId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductOffer>().FirstOrDefaultAsync(x => x.Id == offerId, cancellationToken);

    public async Task<IReadOnlyList<ProductOffer>> ListForProductAsync(
        int productId,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductOffer>()
            .Where(x => x.ProductId == productId && x.VariantId == null);

        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProductOffer>> ListForVariantAsync(
        int variantId,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductOffer>().Where(x => x.VariantId == variantId);

        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductOffer?> FindActiveOfferAsync(
        int productId,
        int? variantId,
        int storeId,
        int currencyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (variantId.HasValue)
        {
            var variantOffer = await FindMatchingOfferAsync(productId, variantId, storeId, currencyId, utcNow, cancellationToken)
                .ConfigureAwait(false);
            if (variantOffer is not null)
            {
                return variantOffer;
            }
        }

        return await FindMatchingOfferAsync(productId, null, storeId, currencyId, utcNow, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ProductOffer offer, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductOffer>().Add(offer);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProductOffer offer, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductOffer>().Update(offer);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task<ProductOffer?> FindMatchingOfferAsync(
        int productId,
        int? variantId,
        int storeId,
        int currencyId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        dbContext.Set<ProductOffer>()
            .Where(x =>
                x.ProductId == productId &&
                x.VariantId == variantId &&
                x.StoreId == storeId &&
                x.CurrencyId == currencyId &&
                x.IsActive &&
                (!x.ValidFromUtc.HasValue || utcNow >= x.ValidFromUtc.Value) &&
                (!x.ValidToUtc.HasValue || utcNow <= x.ValidToUtc.Value))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
}

internal sealed class EfProductMediaRepository(CommerceDbContext dbContext) : IProductMediaRepository
{
    public async Task<IReadOnlyList<ProductMedia>> ListForProductAsync(int productId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ProductMedia>()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductMedia>> ListForProductsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default) =>
        productIds.Count == 0
            ? Array.Empty<ProductMedia>()
            : await dbContext.Set<ProductMedia>()
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

    public Task<ProductMedia?> GetAsync(int productId, int mediaAssetId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductMedia>().FirstOrDefaultAsync(x => x.ProductId == productId && x.MediaAssetId == mediaAssetId, cancellationToken);

    public async Task AddAsync(ProductMedia media, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductMedia>().Add(media);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProductMedia media, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductMedia>().Update(media);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(ProductMedia media, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductMedia>().Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfProductVariantMediaRepository(CommerceDbContext dbContext) : IProductVariantMediaRepository
{
    public async Task<IReadOnlyList<ProductVariantMedia>> ListForVariantAsync(int variantId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ProductVariantMedia>()
            .Where(x => x.VariantId == variantId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductVariantMedia>> ListForVariantsAsync(IReadOnlyCollection<int> variantIds, CancellationToken cancellationToken = default) =>
        variantIds.Count == 0
            ? Array.Empty<ProductVariantMedia>()
            : await dbContext.Set<ProductVariantMedia>()
                .Where(x => variantIds.Contains(x.VariantId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

    public Task<ProductVariantMedia?> GetAsync(int variantId, int mediaAssetId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductVariantMedia>().FirstOrDefaultAsync(x => x.VariantId == variantId && x.MediaAssetId == mediaAssetId, cancellationToken);

    public async Task AddAsync(ProductVariantMedia media, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductVariantMedia>().Add(media);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(ProductVariantMedia media, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductVariantMedia>().Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfCategoryMediaRepository(CommerceDbContext dbContext) : ICategoryMediaRepository
{
    public Task<CategoryMedia?> GetForCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
        dbContext.Set<CategoryMedia>().FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);

    public async Task AddAsync(CategoryMedia media, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CategoryMedia>().Add(media);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(CategoryMedia media, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CategoryMedia>().Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
