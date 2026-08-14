using Commerce.Cart.Application.Abstractions;
using Commerce.Catalog.Contracts.Offers;
using Commerce.Catalog.Contracts.Pricing;
using Commerce.Catalog.Contracts.Products;
using Commerce.Catalog.Contracts.Variants;
using Commerce.Inventory.Contracts.Inventory;

namespace Commerce.Cart.Application.Carts;

public sealed class CartOfferValidator(
    IProductOfferReader offerReader,
    IProductReader productReader,
    IProductVariantReader variantReader,
    ICatalogPricingReader pricingReader,
    IInventoryReader inventoryReader) : ICartOfferValidator
{
    public async Task<OfferValidationResult> ValidateAsync(
        int offerId,
        int storeId,
        int currencyId,
        string currencyCode,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();

        var offerResult = await offerReader.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false);
        if (!offerResult.IsSuccess || offerResult.Value is null)
        {
            return Invalid(offerId, ["Offer was not found."]);
        }

        var offer = offerResult.Value;

        if (offer.StoreId != storeId)
        {
            messages.Add("Offer belongs to a different store.");
        }

        if (offer.CurrencyId != currencyId ||
            !string.Equals(offer.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
        {
            messages.Add("Offer currency does not match the cart currency.");
        }

        if (!offer.IsActive)
        {
            messages.Add("Offer is inactive.");
        }

        var utcNow = DateTime.UtcNow;
        if (offer.ValidFromUtc.HasValue && utcNow < offer.ValidFromUtc.Value)
        {
            messages.Add("Offer is not yet available.");
        }

        if (offer.ValidToUtc.HasValue && utcNow > offer.ValidToUtc.Value)
        {
            messages.Add("Offer has expired.");
        }

        var productResult = await productReader.GetByIdAsync(offer.ProductId, cancellationToken).ConfigureAwait(false);
        if (!productResult.IsSuccess || productResult.Value is null)
        {
            messages.Add("Product was not found.");
            return Invalid(offerId, messages);
        }

        var product = productResult.Value;
        if (!product.Published || !product.IsVisible || !product.IsAvailable || product.Deleted)
        {
            messages.Add("Product is not available for purchase.");
        }

        string? variantName = null;
        string sku = product.Sku;

        if (offer.VariantId.HasValue)
        {
            var variantResult = await variantReader.GetByIdAsync(offer.VariantId.Value, cancellationToken).ConfigureAwait(false);
            if (!variantResult.IsSuccess || variantResult.Value is null)
            {
                messages.Add("Variant was not found.");
            }
            else
            {
                variantName = variantResult.Value.Name;
                sku = variantResult.Value.Sku;
                if (!variantResult.Value.IsActive)
                {
                    messages.Add("Variant is inactive.");
                }
            }
        }

        var resolvedPrice = await pricingReader.GetOfferPriceAsync(offerId, quantity, cancellationToken).ConfigureAwait(false);
        if (resolvedPrice is null)
        {
            messages.Add("Offer price could not be resolved.");
        }

        var inventoryValidation = await inventoryReader
            .ValidateQuantityAsync(offerId, storeId, quantity, cancellationToken)
            .ConfigureAwait(false);

        if (!inventoryValidation.IsValid)
        {
            messages.AddRange(inventoryValidation.Messages);
        }

        var isValid = messages.Count == 0 && resolvedPrice is not null;
        return new OfferValidationResult(
            isValid,
            messages,
            offer.ProductId,
            offer.VariantId,
            product.Name,
            variantName,
            sku,
            resolvedPrice?.UnitPrice ?? offer.Price,
            resolvedPrice?.CurrencyCode ?? offer.CurrencyCode);
    }

    private static OfferValidationResult Invalid(int offerId, IReadOnlyList<string> messages) =>
        new(
            false,
            messages,
            ProductId: 0,
            VariantId: null,
            ProductName: string.Empty,
            VariantName: null,
            Sku: string.Empty,
            UnitPrice: 0m,
            CurrencyCode: string.Empty);
}
