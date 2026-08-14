using Commerce.DisasterRecovery.Application.Abstractions;
using Commerce.DisasterRecovery.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.DisasterRecovery.Infrastructure.Persistence.Repositories;

public sealed class EfBackupRepository(CommerceDbContext dbContext) : IBackupRepository
{
    public async Task AddAsync(BackupRun run, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<BackupRun>().AddAsync(run, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRecoveryTestAsync(RecoveryTestRun test, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<RecoveryTestRun>().AddAsync(test, cancellationToken).ConfigureAwait(false);
    }

    public Task<BackupRun?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Set<BackupRun>()
            .Include(x => x.Artifacts)
            .Include(x => x.RecoveryTests)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BackupRun>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<BackupRun>()
            .Include(x => x.Artifacts)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task DeleteAsync(BackupRun run, CancellationToken cancellationToken = default)
    {
        dbContext.Set<BackupRun>().Remove(run);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
