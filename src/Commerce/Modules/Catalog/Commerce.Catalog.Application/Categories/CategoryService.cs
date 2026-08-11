using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Application.Categories;
using Commerce.Catalog.Contracts.Categories;
using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Services;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Application.Categories;

public interface ICategoryService : ICategoryReader
{
    Task<Result<CategoryDetailDto>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<CategoryDetailDto>> UpdateAsync(int categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int categoryId, CancellationToken cancellationToken = default);
}

public sealed class CategoryService(
    ICategoryRepository categoryRepository,
    IProductCategoryRepository productCategoryRepository) : ICategoryService
{
    public async Task<Result<CategoryDetailDto>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.ParentCategoryId.HasValue &&
                await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, cancellationToken)
                    .ConfigureAwait(false) is null)
            {
                return Result.Failure<CategoryDetailDto>(
                    Error.NotFound($"Parent category '{request.ParentCategoryId}' was not found."));
            }

            Slug? slug = string.IsNullOrWhiteSpace(request.Slug) ? null : Slug.Create(request.Slug);
            var category = Category.Create(
                request.Name,
                request.ParentCategoryId,
                request.Description,
                slug,
                request.Published,
                request.DisplayOrder);

            await categoryRepository.AddAsync(category, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDetailAsync(category, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CategoryDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<CategoryDetailDto>> UpdateAsync(
        int categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result.Failure<CategoryDetailDto>(Error.NotFound($"Category '{categoryId}' was not found."));
        }

        if (request.ParentCategoryId.HasValue &&
            await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result.Failure<CategoryDetailDto>(
                Error.NotFound($"Parent category '{request.ParentCategoryId}' was not found."));
        }

        var categories = await categoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var parentLookup = categories.ToDictionary(x => x.Id, x => x.ParentCategoryId);

        if (CategoryHierarchyValidator.WouldCreateCycle(
                categoryId,
                request.ParentCategoryId,
                id => parentLookup.TryGetValue(id, out var parent) ? parent : null))
        {
            return Result.Failure<CategoryDetailDto>(
                Error.Validation("Category hierarchy update would create a cycle."));
        }

        try
        {
            Slug? slug = string.IsNullOrWhiteSpace(request.Slug) ? null : Slug.Create(request.Slug);
            category.UpdateDetails(
                request.Name,
                request.ParentCategoryId,
                request.Description,
                slug,
                request.Published,
                request.DisplayOrder);

            await categoryRepository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDetailAsync(category, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CategoryDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result.Failure(Error.NotFound($"Category '{categoryId}' was not found."));
        }

        if (await categoryRepository.HasChildrenAsync(categoryId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict("Category cannot be deleted while it has child categories."));
        }

        if (await productCategoryRepository.CategoryHasProductsAsync(categoryId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict("Category cannot be deleted while products are assigned to it."));
        }

        category.MarkDeleted();
        await categoryRepository.DeleteAsync(category, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<CategoryDetailDto>> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result.Failure<CategoryDetailDto>(Error.NotFound($"Category '{categoryId}' was not found."));
        }

        return Result.Success(await MapDetailAsync(category, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<CategorySummaryDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await categoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var summaries = categories
            .Select(c => new CategorySummaryDto(c.Id, c.Name, c.ParentCategoryId, c.Published, c.DisplayOrder, c.Slug))
            .ToList();

        return Result.Success<IReadOnlyList<CategorySummaryDto>>(summaries);
    }

    private async Task<CategoryDetailDto> MapDetailAsync(Category category, CancellationToken cancellationToken)
    {
        var allCategories = await categoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var childIds = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .Select(c => c.Id)
            .ToList();

        var productIds = await productCategoryRepository
            .GetProductIdsForCategoryAsync(category.Id, cancellationToken)
            .ConfigureAwait(false);

        return new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.Published,
            category.DisplayOrder,
            category.Slug,
            category.CreatedAtUtc,
            category.UpdatedAtUtc,
            childIds,
            productIds);
    }
}
