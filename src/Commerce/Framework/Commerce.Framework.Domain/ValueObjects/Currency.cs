using System.Text.RegularExpressions;

namespace Commerce.Framework.Domain.ValueObjects;

public sealed partial record Currency
{
    private static readonly Regex IsoCodePattern = IsoCurrencyCodeRegex();

    public string Code { get; }

    private Currency(string code)
    {
        Code = code;
    }

    public static Currency FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Currency code is required.", nameof(code));
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (!IsoCodePattern.IsMatch(normalized))
        {
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 code.", nameof(code));
        }

        return new Currency(normalized);
    }

    public static Currency Usd => new("USD");
    public static Currency Eur => new("EUR");
    public static Currency Gbp => new("GBP");
    public static Currency Irr => new("IRR");

    public override string ToString() => Code;

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoCurrencyCodeRegex();
}
