using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Application.Attributes;
using Commerce.Catalog.Contracts.Variants;
using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Application.Variants;

public interface IVariantService : IProductVariantReader
{
    Task<Result<VariantDetailDto>> CreateAsync(CreateVariantRequest request, CancellationToken cancellationToken = default);

    Task<Result<VariantDetailDto>> UpdateAsync(
        int variantId,
        UpdateVariantRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int variantId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<VariantSummaryDto>>> GenerateAsync(
        GenerateVariantsRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class VariantService(
    IProductRepository productRepository,
    IProductVariantRepository variantRepository,
    IProductAttributeRepository attributeRepository,
    IAttributeService attributeService) : IVariantService
{
    public async Task<Result<VariantDetailDto>> CreateAsync(
        CreateVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<VariantDetailDto>(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        if (product.ProductType != ProductType.Variant)
        {
            return Result.Failure<VariantDetailDto>(
                Error.Validation("Variants can only be created for products with type Variant."));
        }

        try
        {
            var sku = Sku.Create(request.Sku);
            if (await IsSkuTakenAsync(sku.Value, cancellationToken).ConfigureAwait(false))
            {
                return Result.Failure<VariantDetailDto>(Error.Conflict($"SKU '{sku.Value}' already exists."));
            }

            var optionIds = request.AttributeOptionIds.Distinct().ToList();
            var validation = await ValidateOptionIdsAsync(request.ProductId, optionIds, cancellationToken).ConfigureAwait(false);
            if (!validation.IsSuccess)
            {
                return Result.Failure<VariantDetailDto>(validation.Error!);
            }

            var combinationKey = ProductVariant.BuildCombinationKey(optionIds.OrderBy(x => x).ToList());
            if (await variantRepository.CombinationExistsAsync(request.ProductId, combinationKey, null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Result.Failure<VariantDetailDto>(Error.Conflict("A variant with this attribute combination already exists."));
            }

            var variant = ProductVariant.Create(
                request.ProductId,
                sku,
                request.Name,
                optionIds,
                request.IsActive,
                request.IsDefault,
                request.DisplayOrder);

            await variantRepository.AddAsync(variant, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDetailAsync(variant, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<VariantDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<VariantDetailDto>> UpdateAsync(
        int variantId,
        UpdateVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variant = await variantRepository.GetByIdAsync(variantId, cancellationToken).ConfigureAwait(false);
        if (variant is null)
        {
            return Result.Failure<VariantDetailDto>(Error.NotFound($"Variant '{variantId}' was not found."));
        }

        try
        {
            var optionIds = request.AttributeOptionIds.Distinct().ToList();
            var validation = await ValidateOptionIdsAsync(variant.ProductId, optionIds, cancellationToken).ConfigureAwait(false);
            if (!validation.IsSuccess)
            {
                return Result.Failure<VariantDetailDto>(validation.Error!);
            }

            var combinationKey = ProductVariant.BuildCombinationKey(optionIds.OrderBy(x => x).ToList());
            if (await variantRepository.CombinationExistsAsync(variant.ProductId, combinationKey, variantId, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Result.Failure<VariantDetailDto>(Error.Conflict("A variant with this attribute combination already exists."));
            }

            variant.UpdateDetails(request.Name, request.IsActive, request.IsDefault, request.DisplayOrder, optionIds);
            await variantRepository.UpdateAsync(variant, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDetailAsync(variant, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<VariantDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var variant = await variantRepository.GetByIdAsync(variantId, cancellationToken).ConfigureAwait(false);
        if (variant is null)
        {
            return Result.Failure(Error.NotFound($"Variant '{variantId}' was not found."));
        }

        await variantRepository.DeleteAsync(variant, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<VariantSummaryDto>>> GenerateAsync(
        GenerateVariantsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<IReadOnlyList<VariantSummaryDto>>(
                Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        if (product.ProductType != ProductType.Variant)
        {
            return Result.Failure<IReadOnlyList<VariantSummaryDto>>(
                Error.Validation("Variants can only be generated for products with type Variant."));
        }

        var assignmentsResult = await attributeService.GetForProductAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);
        if (!assignmentsResult.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<VariantSummaryDto>>(assignmentsResult.Error!);
        }

        var optionGroups = assignmentsResult.Value!
            .Where(a => a.Options.Count > 0)
            .Select(a => a.Options.Where(o => o.IsActive).ToList())
            .Where(group => group.Count > 0)
            .ToList();

        if (optionGroups.Count == 0)
        {
            return Result.Failure<IReadOnlyList<VariantSummaryDto>>(
                Error.Validation("Product has no configurable attributes with active options."));
        }

        var combinations = CartesianProduct(optionGroups.Select(g => g.Select(o => o.Id).ToList()).ToList());
        var created = new List<VariantSummaryDto>();
        var displayOrder = 0;
        var isFirst = true;

        foreach (var combination in combinations)
        {
            var sortedIds = combination.OrderBy(x => x).ToList();
            var combinationKey = ProductVariant.BuildCombinationKey(sortedIds);

            if (request.SkipExisting &&
                await variantRepository.CombinationExistsAsync(request.ProductId, combinationKey, null, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            var optionLabels = new List<string>();
            foreach (var optionId in sortedIds)
            {
                var option = await attributeRepository.GetOptionByIdAsync(optionId, cancellationToken).ConfigureAwait(false);
                if (option is not null)
                {
                    optionLabels.Add(option.Value);
                }
            }

            var skuValue = $"{request.SkuPrefix.Trim()}-{string.Join('-', optionLabels.Select(NormalizeSkuPart))}";
            if (await IsSkuTakenAsync(skuValue, cancellationToken).ConfigureAwait(false))
            {
                skuValue = $"{skuValue}-{combinationKey.Replace(':', '-')}";
            }

            try
            {
                var variant = ProductVariant.Create(
                    request.ProductId,
                    Sku.Create(skuValue),
                    $"{product.Name} ({string.Join(", ", optionLabels)})",
                    sortedIds,
                    isActive: true,
                    isDefault: isFirst,
                    displayOrder: displayOrder++);

                await variantRepository.AddAsync(variant, cancellationToken).ConfigureAwait(false);
                created.Add(MapSummary(variant));
                isFirst = false;
            }
            catch (ArgumentException)
            {
                continue;
            }
        }

        return Result.Success<IReadOnlyList<VariantSummaryDto>>(created);
    }

    public async Task<Result<VariantDetailDto>> GetByIdAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var variant = await variantRepository.GetByIdAsync(variantId, cancellationToken).ConfigureAwait(false);
        if (variant is null)
        {
            return Result.Failure<VariantDetailDto>(Error.NotFound($"Variant '{variantId}' was not found."));
        }

        return Result.Success(await MapDetailAsync(variant, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<VariantSummaryDto>>> ListForProductAsync(
        int productId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<IReadOnlyList<VariantSummaryDto>>(
                Error.NotFound($"Product '{productId}' was not found."));
        }

        var variants = await variantRepository.ListForProductAsync(productId, includeInactive, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<VariantSummaryDto>>(variants.Select(MapSummary).ToList());
    }

    // SKU uniqueness is GLOBAL across products and variants (not scoped per store).
    private async Task<bool> IsSkuTakenAsync(string sku, CancellationToken cancellationToken) =>
        await productRepository.GetBySkuAsync(sku, cancellationToken).ConfigureAwait(false) is not null ||
        await variantRepository.GetBySkuAsync(sku, cancellationToken).ConfigureAwait(false) is not null;

    private async Task<Result> ValidateOptionIdsAsync(
        int productId,
        IReadOnlyList<int> optionIds,
        CancellationToken cancellationToken)
    {
        var assignments = await attributeRepository.GetAssignmentsForProductAsync(productId, cancellationToken)
            .ConfigureAwait(false);
        var assignedDefinitionIds = assignments.Select(x => x.AttributeDefinitionId).ToHashSet();

        foreach (var optionId in optionIds)
        {
            var option = await attributeRepository.GetOptionByIdAsync(optionId, cancellationToken).ConfigureAwait(false);
            if (option is null || !option.IsActive)
            {
                return Result.Failure(Error.Validation($"Attribute option '{optionId}' was not found or is inactive."));
            }

            if (!assignedDefinitionIds.Contains(option.AttributeDefinitionId))
            {
                return Result.Failure(Error.Validation(
                    $"Attribute option '{optionId}' is not assigned to product '{productId}'."));
            }
        }

        var definitionIds = new HashSet<int>();
        foreach (var optionId in optionIds)
        {
            var option = await attributeRepository.GetOptionByIdAsync(optionId, cancellationToken).ConfigureAwait(false);
            if (option is not null && !definitionIds.Add(option.AttributeDefinitionId))
            {
                return Result.Failure(Error.Validation("Each attribute definition may contribute at most one option."));
            }
        }

        return Result.Success();
    }

    private async Task<VariantDetailDto> MapDetailAsync(ProductVariant variant, CancellationToken cancellationToken)
    {
        var attributes = new List<VariantAttributeDto>();
        foreach (var attribute in variant.Attributes)
        {
            var option = await attributeRepository.GetOptionByIdAsync(attribute.AttributeOptionId, cancellationToken)
                .ConfigureAwait(false);
            if (option is null)
            {
                continue;
            }

            var definition = await attributeRepository.GetDefinitionByIdAsync(option.AttributeDefinitionId, cancellationToken)
                .ConfigureAwait(false);
            if (definition is null)
            {
                continue;
            }

            attributes.Add(new VariantAttributeDto(
                option.Id,
                definition.Id,
                definition.Code,
                definition.Name,
                option.Value));
        }

        return new VariantDetailDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Name,
            variant.IsActive,
            variant.IsDefault,
            variant.DisplayOrder,
            variant.AttributeCombinationKey,
            attributes,
            variant.CreatedAtUtc,
            variant.UpdatedAtUtc);
    }

    private static VariantSummaryDto MapSummary(ProductVariant variant) =>
        new(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Name,
            variant.IsActive,
            variant.IsDefault,
            variant.DisplayOrder,
            variant.AttributeCombinationKey);

    private static string NormalizeSkuPart(string value) =>
        string.Concat(value.Trim().ToUpperInvariant().Where(c => char.IsLetterOrDigit(c)));

    private static IEnumerable<IReadOnlyList<int>> CartesianProduct(IReadOnlyList<IReadOnlyList<int>> groups)
    {
        IEnumerable<IReadOnlyList<int>> results = [Array.Empty<int>()];
        foreach (var group in groups)
        {
            results = results.SelectMany(
                partial => group.Select(item => partial.Concat([item]).ToList()));
        }

        return results.Where(r => r.Count > 0);
    }
}
