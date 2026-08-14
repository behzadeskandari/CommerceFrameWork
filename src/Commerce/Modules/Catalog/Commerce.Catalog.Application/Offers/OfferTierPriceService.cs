using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Contracts.Offers;
using Commerce.Catalog.Domain.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Catalog.Application.Offers;

public sealed class OfferTierPriceService(
    IOfferTierPriceRepository repository,
    IProductOfferRepository offerRepository) :
    IOfferTierPriceReader,
    IOfferTierPriceAdminService
{
    public Task<decimal?> ResolveTierUnitPriceAsync(
        int offerId,
        int quantity,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        repository.ResolveTierUnitPriceAsync(offerId, quantity, cancellationToken);

    public async Task<IReadOnlyList<OfferTierPriceDto>> ListAsync(int offerId, CancellationToken cancellationToken = default)
    {
        var items = await repository.ListForOfferAsync(offerId, cancellationToken).ConfigureAwait(false);
        return items.Select(Map).ToList();
    }

    public async Task<OfferTierPriceDto> CreateAsync(
        int offerId,
        CreateOfferTierPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        var offer = await offerRepository.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Offer not found.");
        var tier = OfferTierPrice.Create(offerId, request.MinQuantity, Money.Create(request.Price, Currency.FromCode(offer.CurrencyCode)));
        await repository.AddAsync(tier, cancellationToken).ConfigureAwait(false);
        return Map(tier);
    }

    public async Task<OfferTierPriceDto> UpdateAsync(
        int offerId,
        int tierPriceId,
        UpdateOfferTierPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        var offer = await offerRepository.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Offer not found.");
        var tier = await repository.GetByIdAsync(tierPriceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Tier price not found.");
        if (tier.OfferId != offerId)
        {
            throw new InvalidOperationException("Tier price does not belong to the offer.");
        }

        tier.Update(request.MinQuantity, Money.Create(request.Price, Currency.FromCode(offer.CurrencyCode)), request.IsActive);
        await repository.UpdateAsync(tier, cancellationToken).ConfigureAwait(false);
        return Map(tier);
    }

    public async Task DeleteAsync(int offerId, int tierPriceId, CancellationToken cancellationToken = default)
    {
        var tier = await repository.GetByIdAsync(tierPriceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Tier price not found.");
        if (tier.OfferId != offerId)
        {
            throw new InvalidOperationException("Tier price does not belong to the offer.");
        }

        await repository.DeleteAsync(tier, cancellationToken).ConfigureAwait(false);
    }

    private static OfferTierPriceDto Map(OfferTierPrice tier) =>
        new(tier.Id, tier.OfferId, tier.MinQuantity, tier.Price, tier.IsActive);
}
