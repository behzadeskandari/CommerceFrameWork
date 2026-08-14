namespace Commerce.Tax.Application.Tax;

public static class TaxAmountCalculator
{
    public static decimal RoundTax(decimal amount) =>
        Commerce.Framework.Domain.ValueObjects.MonetaryRounding.RoundForTax(amount);

    public static decimal CalculateTaxAmount(
        decimal taxableAmount,
        decimal ratePercentage,
        bool pricesIncludeTax)
    {
        if (taxableAmount <= 0 || ratePercentage <= 0)
        {
            return 0m;
        }

        if (pricesIncludeTax)
        {
            var net = taxableAmount / (1m + ratePercentage / 100m);
            return RoundTax(taxableAmount - net);
        }

        return RoundTax(taxableAmount * ratePercentage / 100m);
    }
}
