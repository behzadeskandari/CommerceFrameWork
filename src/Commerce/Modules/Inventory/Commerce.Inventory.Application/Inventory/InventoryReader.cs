using Commerce.Catalog.Contracts.Offers;
using Commerce.Framework.Core.Results;
using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Inventory;

public sealed class InventoryReader(
    IInventoryRepository inventoryRepository,
    IProductOfferReader offerReader,
    ILogger<InventoryReader> logger) : IInventoryReader, IStorefrontInventoryReader
{
    public async Task<Result<OfferAvailabilityDto>> GetAvailabilityForOfferAsync(
        int offerId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        Result.Success(await ResolveAvailabilityAsync(offerId, storeId, cancellationToken).ConfigureAwait(false));

    public Task<Result<OfferAvailabilityDto>> GetStorefrontAvailabilityAsync(
        int offerId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        GetAvailabilityForOfferAsync(offerId, storeId, cancellationToken);

    public async Task<InventoryValidationResult> ValidateQuantityAsync(
        int offerId,
        int storeId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return new InventoryValidationResult(false, false, ["Quantity must be greater than zero."], null);
        }

        var availability = await ResolveAvailabilityAsync(offerId, storeId, cancellationToken).ConfigureAwait(false);

        if (!availability.TrackInventory)
        {
            return new InventoryValidationResult(true, false, [], availability);
        }

        if (availability.Available >= quantity)
        {
            return new InventoryValidationResult(true, false, [], availability);
        }

        if (availability.AllowBackorder)
        {
            return new InventoryValidationResult(true, true, ["Item is available for backorder."], availability);
        }

        logger.LogInformation(
            "Inventory unavailable for offer {OfferId} in store {StoreId}. Requested {Requested}, available {Available}.",
            offerId,
            storeId,
            quantity,
            availability.Available);

        return new InventoryValidationResult(
            false,
            false,
            [$"Only {availability.Available} unit(s) available."],
            availability);
    }

    private async Task<OfferAvailabilityDto> ResolveAvailabilityAsync(
        int offerId,
        int storeId,
        CancellationToken cancellationToken)
    {
        var item = await inventoryRepository
            .GetByStoreAndOfferAsync(storeId, offerId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            var offerResult = await offerReader.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false);
            if (!offerResult.IsSuccess || offerResult.Value is null)
            {
                return InventoryMapper.NotTracked(storeId, offerId, 0, null);
            }

            return InventoryMapper.NotTracked(
                storeId,
                offerId,
                offerResult.Value.ProductId,
                offerResult.Value.VariantId);
        }

        return InventoryMapper.ToAvailability(item);
    }
}
