using Commerce.Tax.Application.Abstractions;
using Commerce.Tax.Contracts.Tax;
using Commerce.Tax.Domain.Entities;

namespace Commerce.Tax.Application.Tax;

public sealed class InternalTaxProvider(
    ITaxRepository repository,
    TaxSettings settings) : ITaxProvider
{
    public string ProviderSystemName => TaxProviderNames.Internal;

    public async Task<CalculatedTaxResult> CalculateAsync(
        TaxCalculationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!await settings.IsEnabledAsync(context.StoreId, cancellationToken).ConfigureAwait(false))
        {
            return EmptyResult(context);
        }

        if (context.IsCustomerTaxExempt)
        {
            return EmptyResult(context);
        }

        var categories = await repository.GetActiveCategoriesAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var categoryMap = categories.ToDictionary(x => x.Id);
        var defaultCategoryId = await settings.GetDefaultCategoryIdAsync(context.StoreId, cancellationToken).ConfigureAwait(false);

        var zones = await repository.GetActiveZonesAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var matchedZone = TaxZoneMatcher.MatchZone(
            zones,
            context.CountryCode,
            context.StateProvince,
            context.PostalCode);

        var rates = await repository.GetActiveRatesAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var utcNow = DateTime.UtcNow;
        var applicableRates = rates
            .Where(r => r.IsEffective(utcNow))
            .Where(r => r.TaxZoneId == null || (matchedZone is not null && r.TaxZoneId == matchedZone.Id))
            .OrderByDescending(r => r.Priority)
            .ToList();

        var lineItems = new List<CalculatedTaxLineItem>();
        var aggregateLines = new Dictionary<string, CalculatedTaxLine>();

        foreach (var line in context.Lines)
        {
            if (line.TaxableAmount <= 0)
            {
                continue;
            }

            var categoryId = ResolveCategoryId(line.TaxCategoryId, defaultCategoryId);
            if (!categoryId.HasValue)
            {
                continue;
            }

            if (!categoryMap.TryGetValue(categoryId.Value, out var category) || category.IsExempt || !category.IsActive)
            {
                continue;
            }

            var rate = FindRateForCategory(applicableRates, categoryId.Value, taxShipping: false);
            if (rate is null || rate.Percentage <= 0)
            {
                continue;
            }

            var taxAmount = TaxAmountCalculator.CalculateTaxAmount(
                line.TaxableAmount,
                rate.Percentage,
                context.PricesIncludeTax);

            if (taxAmount <= 0)
            {
                continue;
            }

            var rateName = $"{category.Name} ({rate.Percentage}%)";
            lineItems.Add(new CalculatedTaxLineItem(
                line.OfferId,
                category.Id,
                category.Name,
                line.TaxableAmount,
                taxAmount,
                rate.Percentage,
                rateName));

            AddAggregate(aggregateLines, rateName, line.TaxableAmount, taxAmount, rate.Percentage, false, category.Id, category.Name);
        }

        decimal shippingTax = 0m;
        if (context.RequiresShipping && context.ShippingTotal > 0)
        {
            var shippingRate = FindShippingRate(applicableRates, defaultCategoryId, categoryMap);
            if (shippingRate is not null && shippingRate.Percentage > 0)
            {
                shippingTax = TaxAmountCalculator.CalculateTaxAmount(
                    context.ShippingTotal,
                    shippingRate.Percentage,
                    context.PricesIncludeTax);

                if (shippingTax > 0)
                {
                    var name = $"Shipping tax ({shippingRate.Percentage}%)";
                    AddAggregate(aggregateLines, name, context.ShippingTotal, shippingTax, shippingRate.Percentage, true, null, null);
                }
            }
        }

        var productTax = lineItems.Sum(x => x.TaxAmount);
        var lines = aggregateLines.Values.ToList();

        return new CalculatedTaxResult(
            productTax + shippingTax,
            productTax,
            shippingTax,
            context.CurrencyCode,
            context.PricesIncludeTax,
            lines,
            lineItems);
    }

    private static TaxRate? FindRateForCategory(IReadOnlyList<TaxRate> rates, int categoryId, bool taxShipping) =>
        rates
            .Where(r => r.TaxCategoryId == categoryId && r.TaxShipping == taxShipping)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();

    private static TaxRate? FindShippingRate(
        IReadOnlyList<TaxRate> rates,
        int? defaultCategoryId,
        IReadOnlyDictionary<int, TaxCategory> categoryMap)
    {
        var shippingRates = rates.Where(r => r.TaxShipping).OrderByDescending(r => r.Priority).ToList();
        if (shippingRates.Count > 0)
        {
            return shippingRates[0];
        }

        if (defaultCategoryId.HasValue && categoryMap.ContainsKey(defaultCategoryId.Value))
        {
            return FindRateForCategory(rates, defaultCategoryId.Value, taxShipping: true)
                ?? FindRateForCategory(rates, defaultCategoryId.Value, taxShipping: false);
        }

        return null;
    }

    private static int? ResolveCategoryId(int? productCategoryId, int? defaultCategoryId) =>
        productCategoryId ?? defaultCategoryId;

    private static void AddAggregate(
        Dictionary<string, CalculatedTaxLine> aggregate,
        string name,
        decimal taxable,
        decimal tax,
        decimal? rate,
        bool isShipping,
        int? categoryId,
        string? categoryName)
    {
        if (aggregate.TryGetValue(name, out var existing))
        {
            aggregate[name] = existing with
            {
                TaxableAmount = existing.TaxableAmount + taxable,
                TaxAmount = existing.TaxAmount + tax
            };
            return;
        }

        aggregate[name] = new CalculatedTaxLine(name, taxable, tax, rate, isShipping, categoryId, categoryName);
    }

    private static CalculatedTaxResult EmptyResult(TaxCalculationContext context) =>
        new(0m, 0m, 0m, context.CurrencyCode, context.PricesIncludeTax, [], []);
}
