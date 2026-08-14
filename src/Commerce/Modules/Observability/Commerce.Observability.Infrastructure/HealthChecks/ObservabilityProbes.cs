using Commerce.Framework.Contracts.Observability;
using Commerce.Payments.Contracts.Payments;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Commerce.Observability.Infrastructure.HealthChecks;

public sealed class DefaultCacheHealthProbe(IServiceProvider serviceProvider) : ICacheHealthProbe
{
    public async Task<(bool IsHealthy, string Description)> CheckAsync(CancellationToken cancellationToken = default)
    {
        var distributed = serviceProvider.GetService<IDistributedCache>();
        if (distributed is not null)
        {
            var probeKey = $"health:{Guid.NewGuid():N}";
            var bytes = Encoding.UTF8.GetBytes("ok");
            await distributed.SetAsync(probeKey, bytes, cancellationToken).ConfigureAwait(false);
            var stored = await distributed.GetAsync(probeKey, cancellationToken).ConfigureAwait(false);
            await distributed.RemoveAsync(probeKey, cancellationToken).ConfigureAwait(false);
            return stored is not null && Encoding.UTF8.GetString(stored) == "ok"
                ? (true, "Distributed cache probe succeeded.")
                : (false, "Distributed cache probe failed.");
        }

        var memory = serviceProvider.GetService<IMemoryCache>();
        if (memory is not null)
        {
            var probeKey = $"health:{Guid.NewGuid():N}";
            memory.Set(probeKey, "ok", TimeSpan.FromSeconds(5));
            var value = memory.Get<string>(probeKey);
            return value == "ok"
                ? (true, "Memory cache probe succeeded.")
                : (false, "Memory cache probe failed.");
        }

        return (true, "Cache not configured.");
    }
}

public sealed class PaymentProviderHealthProbe(IServiceScopeFactory scopeFactory) : IPaymentProviderHealthProbe
{
    public Task<IReadOnlyList<PaymentProviderHealthEntry>> GetProviderHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var scope = scopeFactory.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IPaymentProvider>();
        var entries = providers
            .Select(provider => new PaymentProviderHealthEntry(
                provider.ProviderSystemName,
                IsRegistered: true,
                IsConfigured: true,
                Message: "Provider registered."))
            .OrderBy(x => x.ProviderSystemName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<PaymentProviderHealthEntry>>(entries);
    }
}
