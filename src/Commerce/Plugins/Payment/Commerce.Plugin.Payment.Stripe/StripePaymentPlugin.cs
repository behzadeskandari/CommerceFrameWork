using Commerce.Framework.PluginContracts.Context;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Payments.Contracts.Callbacks;
using Commerce.Payments.Contracts.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Plugin.Payment.Stripe;

public sealed class StripePaymentPlugin : ICommercePlugin
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<StripeApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        services.AddScoped<IPaymentCallbackHandler, StripeCallbackHandler>();
    }

    public Task InitializeAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StartAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(ICommercePluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
