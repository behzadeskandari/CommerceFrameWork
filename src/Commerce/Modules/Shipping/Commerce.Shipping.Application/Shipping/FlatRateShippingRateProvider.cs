using Commerce.Checkout.Contracts.Checkout;
using Commerce.Shipping.Contracts.Shipping;

namespace Commerce.Shipping.Application.Shipping;

public sealed class FlatRateShippingRateProvider(IShippingCalculationService calculationService) : IShippingRateProvider
{
    public string ProviderSystemName => ShippingProviderNames.FlatRate;

    public async Task<IReadOnlyList<ShippingOption>> GetRatesAsync(
        ShippingRateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ShippingAddress is null)
        {
            return [];
        }

        if (request.ShippingAddress is null)
        {
            return [];
        }

        var context = MapContext(request);
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

    private static ShippingCalculationContext MapContext(ShippingRateRequest request)
    {
        var shippableItems = request.Items
            .Where(x => ShippingCalculationService.IsShippableLine(new ShippingCalculationLineContext(
                x.OfferId, x.ProductId, x.VariantId, x.Quantity, x.UnitPrice,
                x.LineSubtotal, x.ProductType, x.WeightGrams)))
            .ToList();

        return new ShippingCalculationContext(
            request.StoreId,
            request.CurrencyCode,
            request.ShippingAddress.Country,
            request.ShippingAddress.StateProvince,
            request.ShippingAddress.PostalCode,
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
    }
}
