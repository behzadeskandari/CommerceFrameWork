using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Application.Products;
using Commerce.Catalog.Contracts.Products;
using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Application.Products;

public interface IProductService : IProductReader
{
    Task<Result<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<Result<ProductDetailDto>> UpdateAsync(int productId, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int productId, CancellationToken cancellationToken = default);

    Task<Result> AssignCategoryAsync(AssignProductCategoryRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductCategoryRepository productCategoryRepository,
    IProductAttributeRepository attributeRepository,
    IProductVariantRepository variantRepository,
    IEnumerable<ICatalogChangeNotifier> changeNotifiers) : IProductService
{
    public async Task<Result<ProductDetailDto>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var sku = Sku.Create(request.Sku);
            Slug? slug = string.IsNullOrWhiteSpace(request.Slug) ? null : Slug.Create(request.Slug);

            if (await IsSkuTakenAsync(sku.Value, cancellationToken).ConfigureAwait(false))
            {
                return Result.Failure<ProductDetailDto>(Error.Conflict($"Product SKU '{sku.Value}' already exists."));
            }

            var product = Product.Create(
                request.Name,
                sku,
                request.ProductType,
                request.ShortDescription,
                request.Description,
                slug,
                request.Published,
                request.IsVisible,
                request.IsAvailable,
                request.DisplayOrder);

            await productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);

            if (request.CategoryIds is not null)
            {
                foreach (var categoryId in request.CategoryIds.Distinct())
                {
                    if (await categoryRepository.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false) is null)
                    {
                        return Result.Failure<ProductDetailDto>(Error.NotFound($"Category '{categoryId}' was not found."));
                    }

                    await productCategoryRepository.AddAsync(
                        ProductCategory.Create(product.Id, categoryId),
                        cancellationToken).ConfigureAwait(false);
                }
            }

            await NotifyProductCreatedAsync(product.Id, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDetailAsync(product, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<ProductDetailDto>> UpdateAsync(
        int productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<ProductDetailDto>(Error.NotFound($"Product '{productId}' was not found."));
        }

        try
        {
            Slug? slug = string.IsNullOrWhiteSpace(request.Slug) ? null : Slug.Create(request.Slug);
            product.UpdateDetails(
                request.Name,
                request.ProductType,
                request.ShortDescription,
                request.Description,
                slug,
                request.Published,
                request.IsVisible,
                request.IsAvailable,
                request.DisplayOrder);

            await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);

            if (request.CategoryIds is not null)
            {
                var existing = await productCategoryRepository
                    .GetCategoryIdsForProductAsync(productId, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var removeId in existing.Except(request.CategoryIds))
                {
                    await productCategoryRepository.RemoveAsync(productId, removeId, cancellationToken)
                        .ConfigureAwait(false);
                }

                foreach (var addId in request.CategoryIds.Except(existing))
                {
                    if (await categoryRepository.GetByIdAsync(addId, cancellationToken).ConfigureAwait(false) is null)
                    {
                        return Result.Failure<ProductDetailDto>(Error.NotFound($"Category '{addId}' was not found."));
                    }

                    await productCategoryRepository.AddAsync(ProductCategory.Create(productId, addId), cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            await NotifyProductUpdatedAsync(product.Id, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDetailAsync(product, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductDetailDto>(Error.Validation(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ProductDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure(Error.NotFound($"Product '{productId}' was not found."));
        }

        product.SoftDelete();
        await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await NotifyProductDeletedAsync(productId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> AssignCategoryAsync(
        AssignProductCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        if (await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result.Failure(Error.NotFound($"Category '{request.CategoryId}' was not found."));
        }

        if (await productCategoryRepository.ExistsAsync(request.ProductId, request.CategoryId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict("Product is already assigned to this category."));
        }

        await productCategoryRepository.AddAsync(
            ProductCategory.Create(request.ProductId, request.CategoryId, request.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<Result<ProductDetailDto>> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<ProductDetailDto>(Error.NotFound($"Product '{productId}' was not found."));
        }

        return Result.Success(await MapDetailAsync(product, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<ProductSummaryDto>>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var products = await productRepository.ListAsync(includeDeleted, cancellationToken).ConfigureAwait(false);
        var summaries = products
            .Select(MapSummary)
            .ToList();

        return Result.Success<IReadOnlyList<ProductSummaryDto>>(summaries);
    }

    // SKU uniqueness is GLOBAL across products and variants (not scoped per store).
    private async Task<bool> IsSkuTakenAsync(string sku, CancellationToken cancellationToken) =>
        await productRepository.GetBySkuAsync(sku, cancellationToken).ConfigureAwait(false) is not null ||
        await variantRepository.GetBySkuAsync(sku, cancellationToken).ConfigureAwait(false) is not null;

    private async Task<ProductDetailDto> MapDetailAsync(Product product, CancellationToken cancellationToken)
    {
        var categoryIds = await productCategoryRepository
            .GetCategoryIdsForProductAsync(product.Id, cancellationToken)
            .ConfigureAwait(false);

        var attributeValues = await attributeRepository
            .GetValuesForProductAsync(product.Id, cancellationToken)
            .ConfigureAwait(false);

        var mappedAttributes = new List<ProductAttributeValueDto>();
        foreach (var value in attributeValues)
        {
            var definition = await attributeRepository.GetDefinitionByIdAsync(value.AttributeDefinitionId, cancellationToken)
                .ConfigureAwait(false);
            mappedAttributes.Add(new ProductAttributeValueDto(
                value.AttributeDefinitionId,
                definition?.Code ?? string.Empty,
                definition?.Name ?? string.Empty,
                value.Value));
        }

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.ShortDescription,
            product.Description,
            product.Sku,
            product.ProductType.ToString(),
            product.Published,
            product.IsVisible,
            product.IsAvailable,
            product.Deleted,
            product.DisplayOrder,
            product.Slug,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.WeightGrams,
            product.TaxCategoryId,
            categoryIds,
            mappedAttributes);
    }

    private async Task NotifyProductCreatedAsync(int productId, CancellationToken cancellationToken)
    {
        foreach (var notifier in changeNotifiers)
        {
            await notifier.NotifyProductCreatedAsync(productId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task NotifyProductUpdatedAsync(int productId, CancellationToken cancellationToken)
    {
        foreach (var notifier in changeNotifiers)
        {
            await notifier.NotifyProductUpdatedAsync(productId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task NotifyProductDeletedAsync(int productId, CancellationToken cancellationToken)
    {
        foreach (var notifier in changeNotifiers)
        {
            await notifier.NotifyProductDeletedAsync(productId, cancellationToken).ConfigureAwait(false);
        }
    }

    private static ProductSummaryDto MapSummary(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Sku,
            product.ProductType.ToString(),
            product.Published,
            product.IsVisible,
            product.IsAvailable,
            product.Deleted,
            product.DisplayOrder,
            product.Slug);
}
