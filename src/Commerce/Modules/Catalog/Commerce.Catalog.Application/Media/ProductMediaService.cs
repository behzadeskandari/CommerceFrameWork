using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Contracts.Media;
using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Media.Contracts.Media;

namespace Commerce.Catalog.Application.Media;

public sealed class ProductMediaService(
    IProductRepository productRepository,
    IProductVariantRepository variantRepository,
    IProductMediaRepository productMediaRepository,
    IProductVariantMediaRepository variantMediaRepository,
    IMediaReader mediaReader) : IProductMediaService
{
    public async Task<Result<ProductMediaDto>> AssignAsync(
        int productId,
        AssignProductMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<ProductMediaDto>(Error.NotFound($"Product '{productId}' was not found."));
        }

        var media = await mediaReader.GetByIdAsync(request.MediaAssetId, cancellationToken).ConfigureAwait(false);
        if (!media.IsSuccess)
        {
            return Result.Failure<ProductMediaDto>(Error.NotFound($"Media '{request.MediaAssetId}' was not found."));
        }

        if (!Enum.TryParse<ProductMediaRole>(request.Role, true, out var role))
        {
            return Result.Failure<ProductMediaDto>(Error.Validation($"Invalid media role '{request.Role}'."));
        }

        var existing = await productMediaRepository.GetAsync(productId, request.MediaAssetId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            existing.Update(request.DisplayOrder, role);
            await productMediaRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDto(existing, media.Value!));
        }

        var assignment = ProductMedia.Create(productId, request.MediaAssetId, role, request.DisplayOrder);
        await productMediaRepository.AddAsync(assignment, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDto(assignment, media.Value!));
    }

    public async Task<Result> RemoveAsync(int productId, int mediaAssetId, CancellationToken cancellationToken = default)
    {
        var existing = await productMediaRepository.GetAsync(productId, mediaAssetId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(Error.NotFound("Product media assignment was not found."));
        }

        await productMediaRepository.DeleteAsync(existing, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<ProductMediaDto>> AssignVariantMediaAsync(
        int variantId,
        AssignProductMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variant = await variantRepository.GetByIdAsync(variantId, cancellationToken).ConfigureAwait(false);
        if (variant is null)
        {
            return Result.Failure<ProductMediaDto>(Error.NotFound($"Variant '{variantId}' was not found."));
        }

        var media = await mediaReader.GetByIdAsync(request.MediaAssetId, cancellationToken).ConfigureAwait(false);
        if (!media.IsSuccess)
        {
            return Result.Failure<ProductMediaDto>(Error.NotFound($"Media '{request.MediaAssetId}' was not found."));
        }

        if (!Enum.TryParse<ProductMediaRole>(request.Role, true, out var role))
        {
            return Result.Failure<ProductMediaDto>(Error.Validation($"Invalid media role '{request.Role}'."));
        }

        var existing = await variantMediaRepository.GetAsync(variantId, request.MediaAssetId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result.Failure<ProductMediaDto>(Error.Conflict("Variant media assignment already exists."));
        }

        var assignment = ProductVariantMedia.Create(variantId, request.MediaAssetId, role, request.DisplayOrder);
        await variantMediaRepository.AddAsync(assignment, cancellationToken).ConfigureAwait(false);

        return Result.Success(new ProductMediaDto(
            assignment.Id,
            variant.ProductId,
            assignment.MediaAssetId,
            assignment.Role.ToString(),
            assignment.DisplayOrder,
            media.Value!.Url,
            media.Value.ThumbnailUrl,
            media.Value.AltText,
            media.Value.Title));
    }

    public async Task<Result> RemoveVariantMediaAsync(int variantId, int mediaAssetId, CancellationToken cancellationToken = default)
    {
        var existing = await variantMediaRepository.GetAsync(variantId, mediaAssetId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(Error.NotFound("Variant media assignment was not found."));
        }

        await variantMediaRepository.DeleteAsync(existing, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<IReadOnlyList<ProductMediaSummaryDto>> GetForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var items = await productMediaRepository.ListForProductAsync(productId, cancellationToken).ConfigureAwait(false);
        return await MapSummariesAsync(items.Select(x => (x.MediaAssetId, x.Role.ToString(), x.DisplayOrder)).ToList(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, ProductMediaSummaryDto?>> GetPrimaryForProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        var items = await productMediaRepository.ListForProductsAsync(productIds, cancellationToken).ConfigureAwait(false);
        var primaryItems = items
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Role == ProductMediaRole.Primary ? 0 : 1).ThenBy(x => x.DisplayOrder).First());

        var mediaIds = primaryItems.Values.Select(x => x.MediaAssetId).Distinct().ToList();
        var mediaMap = (await mediaReader.GetByIdsAsync(mediaIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(x => x.Id);

        return productIds.ToDictionary(
            id => id,
            id =>
            {
                if (!primaryItems.TryGetValue(id, out var item) || !mediaMap.TryGetValue(item.MediaAssetId, out var media))
                {
                    return null;
                }

                return new ProductMediaSummaryDto(
                    media.Id,
                    item.Role.ToString(),
                    item.DisplayOrder,
                    media.Url,
                    media.ThumbnailUrl,
                    media.AltText);
            });
    }

    public async Task<IReadOnlyList<ProductMediaSummaryDto>> GetForVariantAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var items = await variantMediaRepository.ListForVariantAsync(variantId, cancellationToken).ConfigureAwait(false);
        return await MapSummariesAsync(items.Select(x => (x.MediaAssetId, x.Role.ToString(), x.DisplayOrder)).ToList(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ProductMediaSummaryDto>> MapSummariesAsync(
        IReadOnlyList<(int MediaAssetId, string Role, int DisplayOrder)> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return Array.Empty<ProductMediaSummaryDto>();
        }

        var mediaMap = (await mediaReader.GetByIdsAsync(items.Select(x => x.MediaAssetId).ToList(), cancellationToken).ConfigureAwait(false))
            .ToDictionary(x => x.Id);

        return items
            .Where(x => mediaMap.ContainsKey(x.MediaAssetId))
            .Select(x =>
            {
                var media = mediaMap[x.MediaAssetId];
                return new ProductMediaSummaryDto(
                    media.Id,
                    x.Role,
                    x.DisplayOrder,
                    media.Url,
                    media.ThumbnailUrl,
                    media.AltText);
            })
            .OrderBy(x => x.DisplayOrder)
            .ToList();
    }

    private static ProductMediaDto MapDto(ProductMedia assignment, MediaAssetDto media) =>
        new(
            assignment.Id,
            assignment.ProductId,
            assignment.MediaAssetId,
            assignment.Role.ToString(),
            assignment.DisplayOrder,
            media.Url,
            media.ThumbnailUrl,
            media.AltText,
            media.Title);
}
