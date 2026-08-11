using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Data.Installation;

public sealed class InstallationStateService : IInstallationStateService
{
    private readonly CommerceDbContext _dbContext;
    private readonly IInstallationConnectionProvider _connectionProvider;
    private readonly ILogger<InstallationStateService> _logger;

    public InstallationStateService(
        CommerceDbContext dbContext,
        IInstallationConnectionProvider connectionProvider,
        ILogger<InstallationStateService> logger)
    {
        _dbContext = dbContext;
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<InstallationStateInfo> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (!await CanAccessDatabaseAsync(cancellationToken).ConfigureAwait(false))
        {
            return new InstallationStateInfo(
                InstallationStatus.NotInstalled,
                InstallationStep.Requirements,
                IsLocked: false,
                ApplicationVersion: null,
                InstalledAtUtc: null,
                LastError: null);
        }

        try
        {
            var record = await _dbContext.CommerceInstallations
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (record is null)
            {
                return new InstallationStateInfo(
                    InstallationStatus.NotInstalled,
                    InstallationStep.Requirements,
                    false,
                    null,
                    null,
                    null);
            }

            var status = Enum.TryParse<InstallationStatus>(record.Status, out var parsed)
                ? parsed
                : InstallationStatus.NotInstalled;

            return new InstallationStateInfo(
                status,
                (InstallationStep)record.CurrentStep,
                status == InstallationStatus.Installed,
                record.ApplicationVersion,
                record.InstalledAtUtc,
                record.LastError);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read installation state from database.");
            return new InstallationStateInfo(
                InstallationStatus.Failed,
                InstallationStep.Requirements,
                false,
                null,
                null,
                "Installation state could not be read.");
        }
    }

    public async Task<bool> IsInstalledAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken).ConfigureAwait(false);
        return state.Status == InstallationStatus.Installed;
    }

    public Task<bool> IsInstallationLockedAsync(CancellationToken cancellationToken = default) =>
        IsInstalledAsync(cancellationToken);

    private async Task<bool> CanAccessDatabaseAsync(CancellationToken cancellationToken)
    {
        var connection = _connectionProvider.GetCurrent();
        if (string.IsNullOrWhiteSpace(connection.ConnectionString))
        {
            return false;
        }

        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }
}
