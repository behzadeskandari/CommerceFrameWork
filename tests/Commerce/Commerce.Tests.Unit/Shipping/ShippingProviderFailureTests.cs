using Commerce.Shipping.Application.Shipping;
using Commerce.Shipping.Contracts.Shipping;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commerce.Tests.Unit.Shipping;

public sealed class ShippingProviderFailureTests
{
    [Fact]
    public async Task CalculateOptionsAsync_ContinuesWhenProviderFails()
    {
        var service = new ShippingCalculationService(
        [
            new FailingShippingProvider(),
            new StaticShippingProvider("Shipping.Test", 12m)
        ],
        NullLogger<ShippingCalculationService>.Instance);

        var context = new ShippingCalculationContext(
            1,
            "USD",
            "US",
            "CA",
            "90001",
            100m,
            0m,
            1,
            [new ShippingCalculationLineContext(1, 1, null, 1, 100m, 100m, "Simple", 0m)]);

        var options = await service.CalculateOptionsAsync(context);

        Assert.Single(options);
        Assert.Equal(12m, options[0].Cost);
    }

    private sealed class FailingShippingProvider : IShippingProvider
    {
        public string ProviderSystemName => "Shipping.Fail";

        public Task<IReadOnlyList<CalculatedShippingOption>> GetOptionsAsync(
            ShippingCalculationContext context,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Provider unavailable.");
    }

    private sealed class StaticShippingProvider(string name, decimal cost) : IShippingProvider
    {
        public string ProviderSystemName => name;

        public Task<IReadOnlyList<CalculatedShippingOption>> GetOptionsAsync(
            ShippingCalculationContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CalculatedShippingOption>>(
            [
                new CalculatedShippingOption(
                    "1:Shipping.Test",
                    1,
                    "Test",
                    null,
                    name,
                    cost,
                    context.CurrencyCode,
                    "1 day",
                    0,
                    true)
            ]);
    }
}
