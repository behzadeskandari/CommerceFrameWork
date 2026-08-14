namespace Commerce.Catalog.Contracts.Offers;

public sealed record OfferTierPriceDto(
    int Id,
    int OfferId,
    int MinQuantity,
    decimal Price,
    bool IsActive);

public sealed record CreateOfferTierPriceRequest(
    int MinQuantity,
    decimal Price,
    bool IsActive = true);

public sealed record UpdateOfferTierPriceRequest(
    int MinQuantity,
    decimal Price,
    bool IsActive);

public interface IOfferTierPriceReader
{
    Task<decimal?> ResolveTierUnitPriceAsync(
        int offerId,
        int quantity,
        string currencyCode,
        CancellationToken cancellationToken = default);
}

public interface IOfferTierPriceAdminService
{
    Task<IReadOnlyList<OfferTierPriceDto>> ListAsync(int offerId, CancellationToken cancellationToken = default);
    Task<OfferTierPriceDto> CreateAsync(int offerId, CreateOfferTierPriceRequest request, CancellationToken cancellationToken = default);
    Task<OfferTierPriceDto> UpdateAsync(int offerId, int tierPriceId, UpdateOfferTierPriceRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int offerId, int tierPriceId, CancellationToken cancellationToken = default);
}
