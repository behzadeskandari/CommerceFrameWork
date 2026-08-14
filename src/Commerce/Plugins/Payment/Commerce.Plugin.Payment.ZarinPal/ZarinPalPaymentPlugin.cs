using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Payments.Contracts.Callbacks;
using Commerce.Payments.Contracts.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Payment.ZarinPal;

public sealed class ZarinPalPaymentPlugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ZarinPalApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IPaymentProvider, ZarinPalPaymentProvider>();
        services.AddScoped<IPaymentCallbackHandler, ZarinPalCallbackHandler>();
    }

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
