using Commerce.Checkout.Contracts.Checkout;
using Commerce.Shipping.Contracts.Shipping;

namespace Commerce.Shipping.Application.Shipping;

public sealed class PickupShippingRateProvider(IShippingCalculationService calculationService) : IShippingRateProvider
{
    public string ProviderSystemName => ShippingProviderNames.Pickup;

    public async Task<IReadOnlyList<ShippingOption>> GetRatesAsync(
        ShippingRateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shippableItems = request.Items
            .Where(x => !ShippingCalculationService.IsNonShippableProductType(x.ProductType))
            .ToList();

        if (shippableItems.Count == 0)
        {
            return [];
        }

        var context = new ShippingCalculationContext(
            request.StoreId,
            request.CurrencyCode,
            request.ShippingAddress?.Country ?? string.Empty,
            request.ShippingAddress?.StateProvince,
            request.ShippingAddress?.PostalCode ?? string.Empty,
            shippableItems.Sum(x => x.LineSubtotal),
            shippableItems.Sum(x => x.WeightGrams * x.Quantity),
            shippableItems.Sum(x => x.Quantity),
            shippableItems.Select(x => new ShippingCalculationLineContext(
                x.OfferId,
                x.ProductId,
                x.VariantId,
                x.Quantity,
                x.UnitPrice,
                x.LineSubtotal,
                x.ProductType,
                x.WeightGrams)).ToList());

        var options = await calculationService.CalculateOptionsAsync(context, cancellationToken).ConfigureAwait(false);

        return options
            .Where(x => string.Equals(x.ProviderSystemName, ProviderSystemName, StringComparison.OrdinalIgnoreCase))
            .Select(x => new ShippingOption(
                x.Id,
                x.Name,
                x.ProviderSystemName,
                x.Cost,
                x.CurrencyCode,
                x.EstimatedDelivery,
                x.RequiresAddress))
            .ToList();
    }
}
