using Commerce.Shipping.Application.Abstractions;
using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Domain.Entities;

namespace Commerce.Shipping.Application.Shipping;

public sealed class PickupShippingProvider(
    IShippingRepository repository,
    ShippingSettings settings) : IShippingProvider
{
    public string ProviderSystemName => ShippingProviderNames.Pickup;

    public async Task<IReadOnlyList<CalculatedShippingOption>> GetOptionsAsync(
        ShippingCalculationContext context,
        CancellationToken cancellationToken = default)
    {
        if (!await settings.IsEnabledAsync(context.StoreId, cancellationToken).ConfigureAwait(false))
        {
            return [];
        }

        var methods = await repository.GetActiveMethodsAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var pickupMethods = methods
            .Where(m => string.Equals(m.ProviderSystemName, ProviderSystemName, StringComparison.OrdinalIgnoreCase))
            .Where(m => !m.RequiresAddress)
            .OrderBy(m => m.DisplayOrder)
            .ToList();

        if (pickupMethods.Count == 0)
        {
            return [];
        }

        var rates = await repository.GetActiveRatesAsync(context.StoreId, cancellationToken).ConfigureAwait(false);
        var options = new List<CalculatedShippingOption>();

        foreach (var method in pickupMethods)
        {
            var applicableRates = rates
                .Where(r => r.ShippingMethodId == method.Id)
                .Where(r => r.ShippingZoneId == null)
                .Where(r => string.Equals(r.CurrencyCode, context.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var cost = applicableRates.Count == 0
                ? 0m
                : applicableRates
                    .Select(r => ShippingRateCalculator.CalculateRateCost(r, context, context.CurrencyCode))
                    .DefaultIfEmpty(0m)
                    .Min();

            options.Add(new CalculatedShippingOption(
                $"{method.Id}:{ProviderSystemName}",
                method.Id,
                method.Name,
                method.Description,
                ProviderSystemName,
                cost,
                context.CurrencyCode,
                method.FormatEstimatedDelivery() ?? "Ready for pickup",
                method.DisplayOrder,
                RequiresAddress: false));
        }

        return options;
    }
}
