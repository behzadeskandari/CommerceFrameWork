using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Data.Migrations;

public sealed class MigrationRunner
{
    private readonly CommerceDbContext _dbContext;
    private readonly MigrationRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(
        CommerceDbContext dbContext,
        MigrationRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<MigrationRunner> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ICommerceMigration>> GetPendingMigrationsAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureHistorySchemaAsync(cancellationToken).ConfigureAwait(false);

        var appliedKeys = await _dbContext.MigrationVersionInfo
            .AsNoTracking()
            .Select(x => new { x.Module, x.Version })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var applied = appliedKeys
            .Select(x => (Module: x.Module.ToUpperInvariant(), Version: x.Version))
            .ToHashSet();

        return _registry.GetOrdered()
            .Where(m => !applied.Contains((m.Module.ToUpperInvariant(), m.Version)))
            .ToList();
    }

    public async Task<Result<int>> RunPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);

        if (pending.Count == 0)
        {
            _logger.LogInformation("No pending commerce migrations.");
            return Result.Success(0);
        }

        var executedCount = 0;

        foreach (var migration in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Applying migration {MigrationName} ({Module} v{Version})",
                migration.Name,
                migration.Module,
                migration.Version);

            await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var context = new MigrationExecutionContext(_dbContext, _serviceProvider);
                await migration.UpAsync(context, cancellationToken).ConfigureAwait(false);

                _dbContext.MigrationVersionInfo.Add(new MigrationVersionInfo
                {
                    Version = migration.Version,
                    MigrationName = migration.Name,
                    Module = migration.Module,
                    AppliedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }

                executedCount++;
            }
            catch (Exception ex)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }
                _logger.LogError(
                    ex,
                    "Migration {MigrationName} ({Module} v{Version}) failed.",
                    migration.Name,
                    migration.Module,
                    migration.Version);

                return Result.Failure<int>(
                    Error.Failure(
                        $"Migration '{migration.Name}' failed: {ex.Message}",
                        ErrorCode.OperationFailed,
                        ex.ToString()));
            }
        }

        _logger.LogInformation("Applied {Count} commerce migration(s).", executedCount);
        return Result.Success(executedCount);
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfSupportedAsync(
        CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return null;
        }

        return await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureHistorySchemaAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var created = await _dbContext.Database
            .EnsureCreatedAsync(cancellationToken)
            .ConfigureAwait(false);

        if (created)
        {
            _logger.LogInformation("Commerce database schema created.");
        }
    }
}
