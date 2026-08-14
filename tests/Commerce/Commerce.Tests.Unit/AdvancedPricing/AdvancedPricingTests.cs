using Commerce.Catalog.Domain.Entities;
using Commerce.Framework.Domain.ValueObjects;
using Commerce.Pricing.Domain.Entities;
using Commerce.Tax.Application.Tax;

namespace Commerce.Tests.Unit.AdvancedPricing;

public sealed class MonetaryRoundingTests
{
    [Theory]
    [InlineData(1.23456, 1.2346)]
    [InlineData(1.23444, 1.2344)]
    public void RoundForCalculation_UsesBankersRounding(decimal input, decimal expected) =>
        Assert.Equal(expected, MonetaryRounding.RoundForCalculation(input));

    [Theory]
    [InlineData(10.005, 10.0050)]
    public void RoundForTax_UsesAwayFromZero(decimal input, decimal expected) =>
        Assert.Equal(expected, MonetaryRounding.RoundForTax(input));

    [Fact]
    public void RoundForDisplay_RespectsCurrencyPrecision()
    {
        var rounded = MonetaryRounding.RoundForDisplay(12.3456m, 2);
        Assert.Equal(12.35m, rounded);
    }
}

public sealed class OfferTierPriceDomainTests
{
    [Fact]
    public void Create_StoresQuantityBreakPrice()
    {
        var tier = OfferTierPrice.Create(1, 5, Money.Create(8m, Currency.FromCode("USD")));
        Assert.Equal(5, tier.MinQuantity);
        Assert.Equal(8m, tier.Price);
    }

    [Fact]
    public void Create_RejectsInvalidQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OfferTierPrice.Create(1, 0, Money.Create(8m, Currency.FromCode("USD"))));
    }
}

public sealed class CustomerGroupPriceDomainTests
{
    [Fact]
    public void Create_StoresGroupOverride()
    {
        var price = CustomerGroupPrice.Create(
            1, 1, 10, null, 1, "USD", Money.Create(19.99m, Currency.FromCode("USD")));
        Assert.Equal(19.99m, price.Price);
        Assert.True(price.IsActive);
    }
}

public sealed class TaxAmountCalculatorTests
{
    [Theory]
    [InlineData(100, 10, false, 10)]
    [InlineData(110, 10, true, 10)]
    public void CalculateTaxAmount_SupportsInclusiveAndExclusive(
        decimal taxable,
        decimal rate,
        bool inclusive,
        decimal expectedApprox)
    {
        var tax = TaxAmountCalculator.CalculateTaxAmount(taxable, rate, inclusive);
        Assert.InRange(tax, expectedApprox - 0.01m, expectedApprox + 0.01m);
    }

    [Fact]
    public void CalculateTaxAmount_ReturnsZeroForNonPositiveInputs()
    {
        Assert.Equal(0m, TaxAmountCalculator.CalculateTaxAmount(0m, 10m, false));
        Assert.Equal(0m, TaxAmountCalculator.CalculateTaxAmount(100m, 0m, false));
    }
}
