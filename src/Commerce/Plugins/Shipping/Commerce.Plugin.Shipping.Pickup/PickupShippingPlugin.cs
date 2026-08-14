using Commerce.Checkout.Contracts.Checkout;
using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Shipping.Application.Shipping;
using Commerce.Shipping.Contracts.Shipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Shipping.Pickup;

public sealed class PickupShippingPlugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IShippingProvider, PickupShippingProvider>();
        services.AddScoped<IShippingRateProvider, PickupShippingRateProvider>();
    }

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
