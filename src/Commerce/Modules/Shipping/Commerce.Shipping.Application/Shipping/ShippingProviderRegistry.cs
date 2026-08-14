using Commerce.Shipping.Contracts.Shipments;
using Commerce.Shipping.Contracts.Shipping;

namespace Commerce.Shipping.Application.Shipping;

public sealed class ShippingProviderRegistry(IEnumerable<IShippingProvider> providers) : IShippingProviderRegistry
{
    public IReadOnlyList<ShippingProviderDescriptorDto> ListProviders() =>
        providers
            .Select(x => new ShippingProviderDescriptorDto(x.ProviderSystemName, x.ProviderSystemName, true))
            .DistinctBy(x => x.SystemName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.SystemName, StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public sealed class ShippingProviderResolver(IEnumerable<IShippingProvider> providers)
{
    public IShippingProvider? Resolve(string providerSystemName) =>
        providers.FirstOrDefault(x =>
            string.Equals(x.ProviderSystemName, providerSystemName, StringComparison.OrdinalIgnoreCase));
}
