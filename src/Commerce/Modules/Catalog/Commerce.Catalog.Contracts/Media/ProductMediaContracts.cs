namespace Commerce.Catalog.Contracts.Media;

public sealed record ProductMediaDto(
    int Id,
    int ProductId,
    int MediaAssetId,
    string Role,
    int DisplayOrder,
    string Url,
    string? ThumbnailUrl,
    string? AltText,
    string? Title);

public sealed record AssignProductMediaRequest(
    int MediaAssetId,
    string Role,
    int DisplayOrder = 0);

public sealed record ProductMediaSummaryDto(
    int MediaAssetId,
    string Role,
    int DisplayOrder,
    string Url,
    string? ThumbnailUrl,
    string? AltText);

public sealed record StorefrontMediaDto(
    int MediaAssetId,
    string Url,
    string? ThumbnailUrl,
    string? AltText,
    string Role);

public interface IProductMediaReader
{
    Task<IReadOnlyList<ProductMediaSummaryDto>> GetForProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, ProductMediaSummaryDto?>> GetPrimaryForProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductMediaSummaryDto>> GetForVariantAsync(int variantId, CancellationToken cancellationToken = default);
}

public interface IProductMediaService : IProductMediaReader
{
    Task<Commerce.Framework.Core.Results.Result<ProductMediaDto>> AssignAsync(
        int productId,
        AssignProductMediaRequest request,
        CancellationToken cancellationToken = default);

    Task<Commerce.Framework.Core.Results.Result> RemoveAsync(
        int productId,
        int mediaAssetId,
        CancellationToken cancellationToken = default);

    Task<Commerce.Framework.Core.Results.Result<ProductMediaDto>> AssignVariantMediaAsync(
        int variantId,
        AssignProductMediaRequest request,
        CancellationToken cancellationToken = default);

    Task<Commerce.Framework.Core.Results.Result> RemoveVariantMediaAsync(
        int variantId,
        int mediaAssetId,
        CancellationToken cancellationToken = default);
}
