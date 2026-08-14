using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Data.Deployment;

public sealed class DeploymentStartupHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CommerceDeploymentOptions> deploymentOptions,
    ILogger<DeploymentStartupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = deploymentOptions.Value;

        await using var scope = scopeFactory.CreateAsyncScope();
        var stateService = scope.ServiceProvider.GetRequiredService<IInstallationStateService>();
        var isInstalled = await stateService.IsInstalledAsync(cancellationToken).ConfigureAwait(false);

        if (options.WaitForDatabaseSeconds > 0 && isInstalled)
        {
            await WaitForDatabaseAsync(options, cancellationToken).ConfigureAwait(false);
        }

        if (!options.ApplyMigrationsOnStartup || !isInstalled)
        {
            if (!isInstalled)
            {
                logger.LogInformation(
                    "Skipping startup migrations because commerce is not installed yet.");
            }

            return;
        }

        var migrationRunner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
        var result = await migrationRunner.RunPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            logger.LogError(
                "Startup migration failed: {Error}",
                result.Error?.Message);
            throw new InvalidOperationException(
                $"Startup migration failed: {result.Error?.Message}");
        }

        if (result.Value > 0)
        {
            logger.LogInformation(
                "Applied {Count} pending migration(s) during startup.",
                result.Value);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WaitForDatabaseAsync(
        CommerceDeploymentOptions options,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(options.WaitForDatabaseSeconds);
        var delay = TimeSpan.FromSeconds(Math.Max(1, options.DatabaseRetryDelaySeconds));

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
            try
            {
                if (await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
                {
                    logger.LogInformation("Database connection succeeded during startup wait.");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Database not ready yet.");
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        logger.LogWarning(
            "Database was not reachable within {Seconds}s; continuing startup.",
            options.WaitForDatabaseSeconds);
    }
}
