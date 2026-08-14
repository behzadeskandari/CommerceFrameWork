using Commerce.Tax.Application.Tax;
using Commerce.Tax.Domain.Entities;
using Commerce.Tax.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Tax;

public sealed class TaxZoneMatcherTests
{
    [Fact]
    public void MatchZone_PostalRuleTakesPrecedenceOverCountry()
    {
        var postalZone = CreateZone("postal", isDefault: false, displayOrder: 0);
        postalZone.ReplacePostalRules([TaxZonePostalRule.CreateExact(1, "US", "90210")]);

        var countryZone = CreateZone("country", isDefault: false, displayOrder: 1);
        countryZone.ReplaceCountries([TaxZoneCountry.Create(2, "US")]);

        var match = TaxZoneMatcher.MatchZone([postalZone, countryZone], "US", "CA", "90210");

        Assert.Same(postalZone, match);
    }

    [Fact]
    public void MatchZone_StateMatchesBeforeCountry()
    {
        var stateZone = CreateZone("state", isDefault: false, displayOrder: 0);
        stateZone.ReplaceStates([TaxZoneState.Create(1, "US", "CA")]);

        var countryZone = CreateZone("country", isDefault: false, displayOrder: 1);
        countryZone.ReplaceCountries([TaxZoneCountry.Create(2, "US")]);

        var match = TaxZoneMatcher.MatchZone([stateZone, countryZone], "US", "CA", "90001");

        Assert.Same(stateZone, match);
    }

    [Fact]
    public void MatchZone_DefaultZoneUsedWhenNoGeographicMatch()
    {
        var defaultZone = CreateZone("default", isDefault: true, displayOrder: 99);
        var countryZone = CreateZone("de", isDefault: false, displayOrder: 0);
        countryZone.ReplaceCountries([TaxZoneCountry.Create(2, "DE")]);

        var match = TaxZoneMatcher.MatchZone([defaultZone, countryZone], "US", null, "10001");

        Assert.Same(defaultZone, match);
    }

    private static TaxZone CreateZone(string systemName, bool isDefault, int displayOrder) =>
        TaxZone.Create(1, systemName, systemName, isDefault, isActive: true, displayOrder);
}

public sealed class TaxAmountCalculatorTests
{
    [Fact]
    public void CalculateTaxAmount_ExclusivePricing_AppliesPercentage()
    {
        var tax = TaxAmountCalculator.CalculateTaxAmount(100m, 10m, pricesIncludeTax: false);

        Assert.Equal(10m, tax);
    }

    [Fact]
    public void CalculateTaxAmount_InclusivePricing_ExtractsTaxPortion()
    {
        var tax = TaxAmountCalculator.CalculateTaxAmount(110m, 10m, pricesIncludeTax: true);

        Assert.Equal(10m, tax);
    }

    [Fact]
    public void CalculateTaxAmount_ReturnsZeroForNonPositiveInputs()
    {
        Assert.Equal(0m, TaxAmountCalculator.CalculateTaxAmount(0m, 10m, false));
        Assert.Equal(0m, TaxAmountCalculator.CalculateTaxAmount(100m, 0m, false));
    }
}
