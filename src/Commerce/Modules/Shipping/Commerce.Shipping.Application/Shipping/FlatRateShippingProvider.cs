using Commerce.Shipping.Application.Abstractions;
using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Domain.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Shipping.Application.Shipping;

public static class ShippingRateCalculator
{
    public static decimal CalculateRateCost(
        ShippingRate rate,
        ShippingCalculationContext context,
        string currencyCode)
    {
        if (!rate.IsActive || rate.IsDeleted)
        {
            return 0m;
        }

        if (!string.Equals(rate.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return 0m;
        }

        if (!rate.MatchesOrderSubtotal(context.OrderSubtotal))
        {
            return 0m;
        }

        if (!rate.MatchesWeight(context.TotalWeightGrams))
        {
            return 0m;
        }

        if (rate.FreeShippingThreshold.HasValue && context.OrderSubtotal >= rate.FreeShippingThreshold.Value)
        {
            return 0m;
        }

        var currency = Currency.FromCode(currencyCode);
        var cost = Money.Create(rate.BasePrice, currency);

        if (rate.PricePerWeightUnit.HasValue && context.TotalWeightGrams > 0)
        {
            var weightUnits = context.TotalWeightGrams / 1000m;
            cost = cost.Add(Money.Create(rate.PricePerWeightUnit.Value * weightUnits, currency));
        }

        if (rate.PricePerQuantityUnit.HasValue && context.TotalQuantity > 0)
        {
            cost = cost.Add(Money.Create(rate.PricePerQuantityUnit.Value * context.TotalQuantity, currency));
        }

        if (rate.OrderSubtotalPercentage.HasValue)
        {
            cost = cost.Add(Money.Create(context.OrderSubtotal * rate.OrderSubtotalPercentage.Value / 100m, currency));
        }

        return cost.Amount;
    }
}

public sealed class FlatRateShippingProvider(
    IShippingRepository repository,
    ShippingSettings settings) : IShippingProvider
{
    public string ProviderSystemName => ShippingProviderNames.FlatRate;

    public async Task<IReadOnlyList<CalculatedShippingOption>> GetOptionsAsync(
        ShippingCalculationContext context,
        CancellationToken cancellationToken = default)
    {
        if (!await settings.IsEnabledAsync(context.StoreId, cancellationToken).ConfigureAwait(false))
        {
            return [];
        }

        var methods = await repository.GetActiveMethodsAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var flatMethods = methods
            .Where(m => string.Equals(m.ProviderSystemName, ProviderSystemName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (flatMethods.Count == 0)
        {
            return [];
        }

        var zones = await repository.GetActiveZonesAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var matchedZone = ShippingZoneMatcher.MatchZone(
            zones,
            context.CountryCode,
            context.StateProvince,
            context.PostalCode);

        var rates = await repository.GetActiveRatesAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var options = new List<CalculatedShippingOption>();

        foreach (var method in flatMethods.OrderBy(m => m.DisplayOrder))
        {
            var applicableRates = rates
                .Where(r => r.ShippingMethodId == method.Id)
                .Where(r => r.ShippingZoneId == null || (matchedZone is not null && r.ShippingZoneId == matchedZone.Id))
                .Where(r => string.Equals(r.CurrencyCode, context.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (applicableRates.Count == 0)
            {
                continue;
            }

            var bestRate = applicableRates
                .Select(r => (Rate: r, Cost: ShippingRateCalculator.CalculateRateCost(r, context, context.CurrencyCode)))
                .Where(x => x.Cost >= 0)
                .OrderBy(x => x.Cost)
                .FirstOrDefault();

            if (bestRate.Rate is null)
            {
                continue;
            }

            options.Add(new CalculatedShippingOption(
                $"{method.Id}:{ProviderSystemName}",
                method.Id,
                method.Name,
                method.Description,
                ProviderSystemName,
                bestRate.Cost,
                context.CurrencyCode,
                method.FormatEstimatedDelivery(),
                method.DisplayOrder,
                method.RequiresAddress));
        }

        return options;
    }
}
