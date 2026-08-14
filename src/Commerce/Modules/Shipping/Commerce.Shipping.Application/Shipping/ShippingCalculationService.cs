using Commerce.Catalog.Contracts.Products;
using Commerce.Shipping.Application.Abstractions;
using Commerce.Shipping.Contracts.Shipping;
using Microsoft.Extensions.Logging;

namespace Commerce.Shipping.Application.Shipping;

public sealed class ShippingCalculationService(
    IEnumerable<IShippingProvider> providers,
    ILogger<ShippingCalculationService> logger) : IShippingCalculationService
{
    public async Task<IReadOnlyList<CalculatedShippingOption>> CalculateOptionsAsync(
        ShippingCalculationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var shippableLines = context.Lines.Where(IsShippableLine).ToList();
        if (shippableLines.Count == 0)
        {
            return [];
        }

        var shippableContext = context with
        {
            Lines = shippableLines,
            OrderSubtotal = shippableLines.Sum(x => x.LineSubtotal),
            TotalWeightGrams = shippableLines.Sum(x => x.WeightGrams * x.Quantity),
            TotalQuantity = shippableLines.Sum(x => x.Quantity)
        };

        var options = new List<CalculatedShippingOption>();
        foreach (var provider in providers)
        {
            try
            {
                var providerOptions = await provider.GetOptionsAsync(shippableContext, cancellationToken).ConfigureAwait(false);
                options.AddRange(providerOptions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Shipping provider {ProviderSystemName} failed while calculating options for store {StoreId}.",
                    provider.ProviderSystemName,
                    context.StoreId);
            }
        }

        return options.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Cost).ToList();
    }

    public async Task<CalculatedShippingOption?> ValidateSelectionAsync(
        ShippingCalculationContext context,
        string methodId,
        string providerSystemName,
        CancellationToken cancellationToken = default)
    {
        var options = await CalculateOptionsAsync(context, cancellationToken).ConfigureAwait(false);
        return options.FirstOrDefault(x =>
            x.Id == methodId &&
            string.Equals(x.ProviderSystemName, providerSystemName, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsShippableLine(ShippingCalculationLineContext line) =>
        !IsNonShippableProductType(line.ProductType);

    public static bool IsNonShippableProductType(string productType) =>
        DigitalProductTypes.IsDigital(productType);
}
