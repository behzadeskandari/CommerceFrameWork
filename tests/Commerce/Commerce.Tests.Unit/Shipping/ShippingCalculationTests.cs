using Commerce.Shipping.Application.Shipping;
using Commerce.Shipping.Domain.Entities;
using Commerce.Shipping.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Shipping;

public sealed class ShippingZoneMatcherTests
{
    [Fact]
    public void MatchZone_PostalRuleTakesPrecedenceOverCountry()
    {
        var postalZone = CreateZone("postal", isDefault: false, displayOrder: 0);
        postalZone.ReplacePostalRules([ShippingZonePostalRule.CreateExact(1, "US", "90210")]);

        var countryZone = CreateZone("country", isDefault: false, displayOrder: 1);
        countryZone.ReplaceCountries([ShippingZoneCountry.Create(2, "US")]);

        var match = ShippingZoneMatcher.MatchZone([postalZone, countryZone], "US", "CA", "90210");

        Assert.Same(postalZone, match);
    }

    [Fact]
    public void MatchZone_StateMatchesBeforeCountry()
    {
        var stateZone = CreateZone("state", isDefault: false, displayOrder: 0);
        stateZone.ReplaceStates([ShippingZoneState.Create(1, "US", "CA")]);

        var countryZone = CreateZone("country", isDefault: false, displayOrder: 1);
        countryZone.ReplaceCountries([ShippingZoneCountry.Create(2, "US")]);

        var match = ShippingZoneMatcher.MatchZone([stateZone, countryZone], "US", "CA", "90001");

        Assert.Same(stateZone, match);
    }

    [Fact]
    public void MatchZone_DefaultZoneUsedWhenNoGeographicMatch()
    {
        var defaultZone = CreateZone("default", isDefault: true, displayOrder: 99);
        var countryZone = CreateZone("de", isDefault: false, displayOrder: 0);
        countryZone.ReplaceCountries([ShippingZoneCountry.Create(2, "DE")]);

        var match = ShippingZoneMatcher.MatchZone([defaultZone, countryZone], "US", null, "10001");

        Assert.Same(defaultZone, match);
    }

    private static ShippingZone CreateZone(string systemName, bool isDefault, int displayOrder) =>
        ShippingZone.Create(1, systemName, systemName, isDefault, isActive: true, displayOrder);
}

public sealed class ShippingRateCalculatorTests
{
    [Fact]
    public void CalculateRateCost_AppliesFreeShippingThreshold()
    {
        var rate = ShippingRate.CreateFlat(1, 1, 1, "USD", 15m, freeShippingThreshold: 100m, null, null);
        var context = CreateContext(orderSubtotal: 120m);

        var cost = ShippingRateCalculator.CalculateRateCost(rate, context, "USD");

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void CalculateRateCost_AddsWeightSurcharge()
    {
        var rate = ShippingRate.CreateFlat(1, 1, 1, "USD", 10m, null, null, null, pricePerWeightUnit: 5m);
        var context = CreateContext(orderSubtotal: 50m, weightGrams: 2000m);

        var cost = ShippingRateCalculator.CalculateRateCost(rate, context, "USD");

        Assert.Equal(20m, cost);
    }

    [Fact]
    public void CalculateRateCost_AppliesOrderSubtotalPercentage()
    {
        var rate = ShippingRate.CreateOrderSubtotalBased(1, 1, 1, "USD", 5m, 10m, null, null);
        var context = CreateContext(orderSubtotal: 200m);

        var cost = ShippingRateCalculator.CalculateRateCost(rate, context, "USD");

        Assert.Equal(25m, cost);
    }

    [Fact]
    public void CalculateRateCost_AppliesQuantityBasedRate()
    {
        var rate = ShippingRate.CreateQuantityBased(1, 1, 1, "USD", 2m, 1.5m);
        var context = CreateContext(orderSubtotal: 50m) with { TotalQuantity = 4 };

        var cost = ShippingRateCalculator.CalculateRateCost(rate, context, "USD");

        Assert.Equal(8m, cost);
    }

    [Fact]
    public void CalculateRateCost_RespectsMaxWeightBand()
    {
        var rate = ShippingRate.CreateWeightBased(1, 1, 1, "USD", 10m, 1m, null, 500m);
        var context = CreateContext(orderSubtotal: 20m, weightGrams: 2000m);

        var cost = ShippingRateCalculator.CalculateRateCost(rate, context, "USD");

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void CalculateRateCost_ReturnsZeroWhenCurrencyMismatch()
    {
        var rate = ShippingRate.CreateFlat(1, 1, 1, "USD", 10m, null, null, null);
        var context = CreateContext(orderSubtotal: 50m);

        var cost = ShippingRateCalculator.CalculateRateCost(rate, context, "EUR");

        Assert.Equal(0m, cost);
    }

    private static global::Commerce.Shipping.Contracts.Shipping.ShippingCalculationContext CreateContext(
        decimal orderSubtotal,
        decimal weightGrams = 0m) =>
        new(1, "USD", "US", "CA", "90001", orderSubtotal, weightGrams, 1, []);
}
