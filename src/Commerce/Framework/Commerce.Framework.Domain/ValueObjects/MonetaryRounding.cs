namespace Commerce.Framework.Domain.ValueObjects;

public static class MonetaryRounding
{
    public const int CalculationDecimalPlaces = Money.DefaultScale;
    public const int TaxDecimalPlaces = 4;

    public static decimal RoundForCalculation(decimal amount) =>
        decimal.Round(amount, CalculationDecimalPlaces, MidpointRounding.ToEven);

    public static decimal RoundForTax(decimal amount) =>
        decimal.Round(amount, TaxDecimalPlaces, MidpointRounding.AwayFromZero);

    public static decimal RoundForDisplay(decimal amount, int decimalPlaces)
    {
        if (decimalPlaces < 0 || decimalPlaces > CalculationDecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
        }

        return decimal.Round(amount, decimalPlaces, MidpointRounding.ToEven);
    }

    public static Money RoundMoney(Money money, int displayDecimalPlaces) =>
        Money.Create(RoundForDisplay(money.Amount, displayDecimalPlaces), money.Currency);
}
