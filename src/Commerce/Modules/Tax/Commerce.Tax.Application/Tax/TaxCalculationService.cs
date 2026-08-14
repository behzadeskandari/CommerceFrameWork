using Commerce.Tax.Contracts.Tax;

namespace Commerce.Tax.Application.Tax;

public sealed class TaxCalculationService(IEnumerable<ITaxProvider> providers) : ITaxCalculationService
{
    public async Task<CalculatedTaxResult> CalculateAsync(
        TaxCalculationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var provider in providers)
        {
            var result = await provider.CalculateAsync(context, cancellationToken).ConfigureAwait(false);
            if (result.TaxTotal > 0 || provider.ProviderSystemName == TaxProviderNames.Internal)
            {
                return result;
            }
        }

        return new CalculatedTaxResult(0m, 0m, 0m, context.CurrencyCode, context.PricesIncludeTax, [], []);
    }
}
