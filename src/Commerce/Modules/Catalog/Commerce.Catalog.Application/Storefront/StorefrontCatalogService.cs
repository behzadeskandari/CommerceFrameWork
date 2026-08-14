using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Application.Attributes;
using Commerce.Catalog.Contracts.Media;
using Commerce.Catalog.Contracts.Pricing;
using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Application.Storefront;

public interface IStorefrontCatalogService
{
    Task<Result<IReadOnlyList<ProductSummaryDto>>> ListProductsAsync(
        string? term = null,
        CancellationToken cancellationToken = default);

    Task<Result<StorefrontProductDetailDto>> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<Result<StorefrontProductDetailDto>> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}

public sealed class StorefrontCatalogService(
    IProductRepository productRepository,
    IProductCategoryRepository productCategoryRepository,
    IProductVariantRepository variantRepository,
    IProductAttributeRepository attributeRepository,
    IAttributeService attributeService,
    IPricingService pricingService,
    IProductMediaService productMediaService) : IStorefrontCatalogService
{
    public async Task<Result<IReadOnlyList<ProductSummaryDto>>> ListProductsAsync(
        string? term = null,
        CancellationToken cancellationToken = default)
    {
        var products = await productRepository.SearchAsync(term, publicOnly: true, cancellationToken).ConfigureAwait(false);
        var productIds = products.Select(p => p.Id).ToList();
        var primaryImages = await productMediaService.GetPrimaryForProductsAsync(productIds, cancellationToken).ConfigureAwait(false);

        var summaries = products
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.Sku,
                p.ProductType.ToString(),
                p.Published,
                p.IsVisible,
                p.IsAvailable,
                p.Deleted,
                p.DisplayOrder,
                p.Slug,
                primaryImages.GetValueOrDefault(p.Id)))
            .ToList();

        return Result.Success<IReadOnlyList<ProductSummaryDto>>(summaries);
    }

    public async Task<Result<StorefrontProductDetailDto>> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || !product.IsPubliclyVisible())
        {
            return Result.Failure<StorefrontProductDetailDto>(Error.NotFound($"Product '{productId}' was not found."));
        }

        return Result.Success(await MapStorefrontDetailAsync(product, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<StorefrontProductDetailDto>> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result.Failure<StorefrontProductDetailDto>(Error.Validation("Slug is required."));
        }

        var product = await productRepository.GetBySlugAsync(slug.Trim(), cancellationToken).ConfigureAwait(false);
        if (product is null || !product.IsPubliclyVisible())
        {
            return Result.Failure<StorefrontProductDetailDto>(Error.NotFound($"Product slug '{slug}' was not found."));
        }

        return Result.Success(await MapStorefrontDetailAsync(product, cancellationToken).ConfigureAwait(false));
    }

    private async Task<StorefrontProductDetailDto> MapStorefrontDetailAsync(
        Domain.Entities.Product product,
        CancellationToken cancellationToken)
    {
        var categoryIds = await productCategoryRepository
            .GetCategoryIdsForProductAsync(product.Id, cancellationToken)
            .ConfigureAwait(false);

        var assignmentsResult = await attributeService.GetForProductAsync(product.Id, cancellationToken)
            .ConfigureAwait(false);
        var configurableAttributes = assignmentsResult.IsSuccess
            ? assignmentsResult.Value!
                .Select(a => new ProductAttributeAssignmentSummaryDto(
                    a.AttributeDefinitionId,
                    a.AttributeCode,
                    a.AttributeName,
                    a.Options.Select(o => new StorefrontAttributeOptionDto(o.Id, o.Value)).ToList()))
                .ToList()
            : [];

        var variants = await variantRepository.ListForProductAsync(product.Id, includeInactive: false, cancellationToken)
            .ConfigureAwait(false);

        var productMedia = await productMediaService.GetForProductAsync(product.Id, cancellationToken).ConfigureAwait(false);
        var gallery = productMedia
            .Select(m => new StorefrontMediaDto(m.MediaAssetId, m.Url, m.ThumbnailUrl, m.AltText, m.Role))
            .ToList();
        var primaryImage = gallery.FirstOrDefault(x => x.Role.Equals("Primary", StringComparison.OrdinalIgnoreCase))
            ?? gallery.FirstOrDefault();

        var variantIds = variants.Select(v => v.Id).ToList();
        var variantMediaMap = new Dictionary<int, IReadOnlyList<ProductMediaSummaryDto>>();
        foreach (var variantId in variantIds)
        {
            variantMediaMap[variantId] = await productMediaService.GetForVariantAsync(variantId, cancellationToken).ConfigureAwait(false);
        }

        var optionIds = variants
            .SelectMany(v => v.Attributes)
            .Select(a => a.AttributeOptionId)
            .Distinct()
            .ToList();
        var optionLookup = (await attributeRepository.GetOptionsByIdsAsync(optionIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(x => x.Id);

        var storefrontVariants = new List<StorefrontVariantDto>();
        foreach (var variant in variants)
        {
            var options = new List<StorefrontAttributeOptionDto>();
            foreach (var attribute in variant.Attributes)
            {
                if (optionLookup.TryGetValue(attribute.AttributeOptionId, out var option))
                {
                    options.Add(new StorefrontAttributeOptionDto(option.Id, option.Value));
                }
            }

            StorefrontMediaDto? variantImage = null;
            if (variantMediaMap.TryGetValue(variant.Id, out var vMedia) && vMedia.Count > 0)
            {
                var first = vMedia[0];
                variantImage = new StorefrontMediaDto(first.MediaAssetId, first.Url, first.ThumbnailUrl, first.AltText, first.Role);
            }

            storefrontVariants.Add(new StorefrontVariantDto(
                variant.Id,
                variant.Sku,
                variant.Name,
                variant.IsDefault,
                options,
                variantImage));
        }

        var defaultVariant = variants.FirstOrDefault(v => v.IsDefault) ?? variants.FirstOrDefault();
        ResolvedPriceSummaryDto? price = null;

        if (defaultVariant is not null)
        {
            var variantPrice = await pricingService.ResolveVariantPriceAsync(defaultVariant.Id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (variantPrice.IsSuccess)
            {
                price = MapPriceSummary(variantPrice.Value!);
            }
        }
        else
        {
            var productPrice = await pricingService.ResolveProductPriceAsync(product.Id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (productPrice.IsSuccess)
            {
                price = MapPriceSummary(productPrice.Value!);
            }
        }

        return new StorefrontProductDetailDto(
            product.Id,
            product.Name,
            product.ShortDescription,
            product.Description,
            product.Sku,
            product.ProductType.ToString(),
            product.Slug,
            categoryIds,
            configurableAttributes,
            storefrontVariants,
            defaultVariant?.Id,
            price,
            primaryImage,
            gallery);
    }

    private static ResolvedPriceSummaryDto MapPriceSummary(Contracts.Pricing.ResolvedPriceDto price) =>
        new(price.OfferId, price.CurrencyCode, price.UnitPrice, price.CompareAtPrice, price.Availability);
}
