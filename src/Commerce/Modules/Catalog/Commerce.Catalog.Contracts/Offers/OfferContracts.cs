using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Contracts.Offers;

public sealed record OfferSummaryDto(
    int Id,
    int ProductId,
    int? VariantId,
    int StoreId,
    int CurrencyId,
    string CurrencyCode,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc);

public sealed record OfferDetailDto(
    int Id,
    int ProductId,
    int? VariantId,
    int StoreId,
    int CurrencyId,
    string CurrencyCode,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public interface IProductOfferReader
{
    Task<Result<OfferDetailDto>> GetByIdAsync(int offerId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OfferSummaryDto>>> ListForProductAsync(
        int productId,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OfferSummaryDto>>> ListForVariantAsync(
        int variantId,
        int? storeId = null,
        CancellationToken cancellationToken = default);
}
