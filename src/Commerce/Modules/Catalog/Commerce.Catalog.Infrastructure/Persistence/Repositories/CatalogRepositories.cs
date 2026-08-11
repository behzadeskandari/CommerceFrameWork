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
    public Task<ProductAttributeDefinition?> GetDefinitionByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProductAttributeDefinition>()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task AddDefinitionAsync(ProductAttributeDefinition definition, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ProductAttributeDefinition>().Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
