using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Application.Offers;
using Commerce.Catalog.Contracts.Offers;
using Commerce.Catalog.Domain.Entities;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Catalog.Application.Offers;

public interface IOfferService : IProductOfferReader
{
    Task<Result<OfferDetailDto>> CreateAsync(CreateOfferRequest request, CancellationToken cancellationToken = default);

    Task<Result<OfferDetailDto>> UpdateAsync(
        int offerId,
        UpdateOfferRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class OfferService(
    IProductRepository productRepository,
    IProductVariantRepository variantRepository,
    IProductOfferRepository offerRepository) : IOfferService
{
    public async Task<Result<OfferDetailDto>> CreateAsync(
        CreateOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<OfferDetailDto>(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        if (request.VariantId.HasValue)
        {
            var variant = await variantRepository.GetByIdAsync(request.VariantId.Value, cancellationToken).ConfigureAwait(false);
            if (variant is null || variant.ProductId != request.ProductId)
            {
                return Result.Failure<OfferDetailDto>(Error.NotFound($"Variant '{request.VariantId}' was not found."));
            }
        }

        try
        {
            var currency = Currency.FromCode(request.CurrencyCode);
            var price = Money.Create(request.Price, currency);
            var compareAt = request.CompareAtPrice.HasValue ? Money.Create(request.CompareAtPrice.Value, currency) : null;

            var offer = ProductOffer.Create(
                request.ProductId,
                request.VariantId,
                request.StoreId,
                request.CurrencyId,
                request.CurrencyCode,
                price,
                compareAt,
                request.IsActive,
                request.ValidFromUtc,
                request.ValidToUtc);

            await offerRepository.AddAsync(offer, cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(offer));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<OfferDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<OfferDetailDto>> UpdateAsync(
        int offerId,
        UpdateOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var offer = await offerRepository.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false);
        if (offer is null)
        {
            return Result.Failure<OfferDetailDto>(Error.NotFound($"Offer '{offerId}' was not found."));
        }

        try
        {
            var currency = Currency.FromCode(offer.CurrencyCode);
            var price = Money.Create(request.Price, currency);
            var compareAt = request.CompareAtPrice.HasValue ? Money.Create(request.CompareAtPrice.Value, currency) : null;

            offer.Update(price, compareAt, request.IsActive, request.ValidFromUtc, request.ValidToUtc);
            await offerRepository.UpdateAsync(offer, cancellationToken).ConfigureAwait(false);
            return Result.Success(MapDetail(offer));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<OfferDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<OfferDetailDto>> GetByIdAsync(int offerId, CancellationToken cancellationToken = default)
    {
        var offer = await offerRepository.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false);
        if (offer is null)
        {
            return Result.Failure<OfferDetailDto>(Error.NotFound($"Offer '{offerId}' was not found."));
        }

        return Result.Success(MapDetail(offer));
    }

    public async Task<Result<IReadOnlyList<OfferSummaryDto>>> ListForProductAsync(
        int productId,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<IReadOnlyList<OfferSummaryDto>>(
                Error.NotFound($"Product '{productId}' was not found."));
        }

        var offers = await offerRepository.ListForProductAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<OfferSummaryDto>>(offers.Select(MapSummary).ToList());
    }

    public async Task<Result<IReadOnlyList<OfferSummaryDto>>> ListForVariantAsync(
        int variantId,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var variant = await variantRepository.GetByIdAsync(variantId, cancellationToken).ConfigureAwait(false);
        if (variant is null)
        {
            return Result.Failure<IReadOnlyList<OfferSummaryDto>>(
                Error.NotFound($"Variant '{variantId}' was not found."));
        }

        var offers = await offerRepository.ListForVariantAsync(variantId, storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<OfferSummaryDto>>(offers.Select(MapSummary).ToList());
    }

    private static OfferSummaryDto MapSummary(ProductOffer offer) =>
        new(
            offer.Id,
            offer.ProductId,
            offer.VariantId,
            offer.StoreId,
            offer.CurrencyId,
            offer.CurrencyCode,
            offer.Price,
            offer.CompareAtPrice,
            offer.IsActive,
            offer.ValidFromUtc,
            offer.ValidToUtc);

    private static OfferDetailDto MapDetail(ProductOffer offer) =>
        new(
            offer.Id,
            offer.ProductId,
            offer.VariantId,
            offer.StoreId,
            offer.CurrencyId,
            offer.CurrencyCode,
            offer.Price,
            offer.CompareAtPrice,
            offer.IsActive,
            offer.ValidFromUtc,
            offer.ValidToUtc,
            offer.CreatedAtUtc,
            offer.UpdatedAtUtc);
}
