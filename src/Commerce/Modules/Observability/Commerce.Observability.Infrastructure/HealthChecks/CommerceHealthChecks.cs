using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.PluginContracts.Lifecycle;
using Commerce.Framework.PluginContracts.Plugins;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Commerce.Observability.Infrastructure.HealthChecks;

public sealed class LivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Process is alive."));
}

public sealed class DatabaseHealthCheck(Commerce.Framework.Data.Db.CommerceDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
            return canConnect
                ? HealthCheckResult.Healthy("Database connection succeeded.")
                : HealthCheckResult.Unhealthy("Database connection failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}

public sealed class CacheHealthCheck(Commerce.Framework.Contracts.Observability.ICacheHealthProbe cacheProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var (isHealthy, description) = await cacheProbe.CheckAsync(cancellationToken).ConfigureAwait(false);
        return isHealthy
            ? HealthCheckResult.Healthy(description)
            : HealthCheckResult.Degraded(description);
    }
}

public sealed class SchedulingHealthCheck(Commerce.Framework.Contracts.Observability.ISchedulingHealthProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await probe.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var data = new Dictionary<string, object>
        {
            ["pendingJobs"] = snapshot.PendingJobCount,
            ["deadLetterJobs"] = snapshot.DeadLetterCount,
            ["staleLocks"] = snapshot.StaleLockCount,
            ["lastCycleUtc"] = snapshot.LastCycleUtc?.ToString("O") ?? "unknown"
        };

        if (!snapshot.ProcessorRunning)
        {
            return HealthCheckResult.Degraded("Background job processor has not completed a cycle.", data: data);
        }

        if (snapshot.DeadLetterCount > 0)
        {
            return HealthCheckResult.Degraded("Dead-letter background jobs detected.", data: data);
        }

        return HealthCheckResult.Healthy("Background job processor is healthy.", data);
    }
}

public sealed class PluginHealthCheck(ICommercePluginManager pluginManager) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var plugins = pluginManager.Discover();
        var failedRequired = plugins
            .Where(x => x.Descriptor.IsRequired && x.State == PluginState.Failed)
            .Select(x => x.Descriptor.SystemName)
            .ToList();

        var data = new Dictionary<string, object>
        {
            ["total"] = plugins.Count,
            ["failedRequired"] = failedRequired
        };

        if (failedRequired.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Required plugins failed: {string.Join(", ", failedRequired)}.",
                data: data));
        }

        var failedOptional = plugins.Count(x => x.State == PluginState.Failed);
        data["failedOptional"] = failedOptional;
        return Task.FromResult(failedOptional > 0
            ? HealthCheckResult.Degraded("One or more optional plugins failed.", data: data)
            : HealthCheckResult.Healthy("Plugins are healthy.", data));
    }
}

public sealed class ModuleHealthCheck(ICommerceModuleRegistry moduleRegistry) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var modules = moduleRegistry.GetModules();
        var failedRequired = modules
            .Where(x => x.Descriptor.IsRequired && x.State == ModuleState.Failed)
            .Select(x => x.Descriptor.SystemName)
            .ToList();

        var data = new Dictionary<string, object>
        {
            ["total"] = modules.Count,
            ["failedRequired"] = failedRequired
        };

        if (failedRequired.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Required modules failed: {string.Join(", ", failedRequired)}.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Modules are healthy.", data));
    }
}

public sealed class PaymentProviderHealthCheck(Commerce.Framework.Contracts.Observability.IPaymentProviderHealthProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = await probe.GetProviderHealthAsync(cancellationToken).ConfigureAwait(false);
        var data = new Dictionary<string, object>
        {
            ["providers"] = providers.Select(x => new
            {
                x.ProviderSystemName,
                x.IsRegistered,
                x.IsConfigured,
                x.Message
            }).ToList()
        };

        if (providers.Count == 0)
        {
            return HealthCheckResult.Degraded("No payment providers registered.", data: data);
        }

        var misconfigured = providers.Where(x => x.IsRegistered && !x.IsConfigured).ToList();
        if (misconfigured.Count > 0)
        {
            return HealthCheckResult.Degraded(
                "One or more payment providers are not configured.",
                data: data);
        }

        return HealthCheckResult.Healthy("Payment providers are healthy.", data);
    }
}

public sealed class BackupHealthCheck(Commerce.Framework.Contracts.Observability.IBackupHealthProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await probe.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var data = new Dictionary<string, object>
        {
            ["backupFresh"] = snapshot.BackupFresh,
            ["restoreTested"] = snapshot.RestoreTested,
            ["latestBackupAtUtc"] = snapshot.LatestBackupAtUtc?.ToString("O") ?? "none",
            ["latestValidityStatus"] = snapshot.LatestValidityStatus ?? "none"
        };

        if (!snapshot.BackupFresh)
        {
            return HealthCheckResult.Unhealthy(snapshot.Message, data: data);
        }

        if (!snapshot.RestoreTested)
        {
            return HealthCheckResult.Degraded(snapshot.Message, data: data);
        }

        return HealthCheckResult.Healthy(snapshot.Message, data: data);
    }
}

public sealed class ReadinessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Readiness probe endpoint is active."));
}
