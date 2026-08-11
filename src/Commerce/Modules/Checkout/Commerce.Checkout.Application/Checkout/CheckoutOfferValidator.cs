using Commerce.Catalog.Contracts.Offers;
using Commerce.Catalog.Contracts.Pricing;
using Commerce.Catalog.Contracts.Products;
using Commerce.Catalog.Contracts.Variants;
using Commerce.Inventory.Contracts.Inventory;

namespace Commerce.Checkout.Application.Checkout;

public sealed record CheckoutLineValidation(
    bool IsValid,
    IReadOnlyList<string> Messages,
    int ProductId,
    int? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    string ProductType,
    decimal UnitPrice,
    decimal PreviousUnitPrice,
    string CurrencyCode);

public interface ICheckoutOfferValidator
{
    Task<CheckoutLineValidation> ValidateLineAsync(
        int offerId,
        int quantity,
        int storeId,
        int currencyId,
        string currencyCode,
        decimal? previousUnitPrice,
        CancellationToken cancellationToken = default);
}

public sealed class CheckoutOfferValidator(
    IProductOfferReader offerReader,
    IProductReader productReader,
    IProductVariantReader variantReader,
    ICatalogPricingReader pricingReader,
    IInventoryReader inventoryReader) : ICheckoutOfferValidator
{
    public async Task<CheckoutLineValidation> ValidateLineAsync(
        int offerId,
        int quantity,
        int storeId,
        int currencyId,
        string currencyCode,
        decimal? previousUnitPrice,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();

        var offerResult = await offerReader.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false);
        if (!offerResult.IsSuccess || offerResult.Value is null)
        {
            return Invalid(["Offer was not found."], previousUnitPrice);
        }

        var offer = offerResult.Value;
        if (offer.StoreId != storeId)
        {
            messages.Add("Offer belongs to a different store.");
        }

        if (offer.CurrencyId != currencyId ||
            !string.Equals(offer.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
        {
            messages.Add("Offer currency does not match checkout currency.");
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
            return Invalid(messages, previousUnitPrice);
        }

        var product = productResult.Value;
        if (!product.Published || !product.IsVisible || !product.IsAvailable || product.Deleted)
        {
            messages.Add("Product is not available for purchase.");
        }

        string? variantName = null;
        var sku = product.Sku;
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

        var resolvedPrice = await pricingReader.GetOfferPriceAsync(offerId, cancellationToken).ConfigureAwait(false);
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

        var unitPrice = resolvedPrice?.UnitPrice ?? offer.Price;
        var isValid = messages.Count == 0 && resolvedPrice is not null;

        return new CheckoutLineValidation(
            isValid,
            messages,
            offer.ProductId,
            offer.VariantId,
            product.Name,
            variantName,
            sku,
            product.ProductType,
            unitPrice,
            previousUnitPrice ?? unitPrice,
            resolvedPrice?.CurrencyCode ?? offer.CurrencyCode);
    }

    private static CheckoutLineValidation Invalid(IReadOnlyList<string> messages, decimal? previousUnitPrice) =>
        new(
            false,
            messages,
            0,
            null,
            string.Empty,
            null,
            string.Empty,
            string.Empty,
            0m,
            previousUnitPrice ?? 0m,
            string.Empty);
}
