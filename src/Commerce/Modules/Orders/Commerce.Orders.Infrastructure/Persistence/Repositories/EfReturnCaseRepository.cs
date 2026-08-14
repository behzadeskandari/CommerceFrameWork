using Commerce.Framework.Data.Db;
using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Orders.Infrastructure.Persistence.Repositories;

public sealed class EfReturnCaseRepository(CommerceDbContext dbContext) : IReturnCaseRepository
{
    public Task<ReturnCase?> GetByIdWithItemsAsync(int returnCaseId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ReturnCase>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == returnCaseId, cancellationToken);

    public async Task<IReadOnlyList<ReturnCase>> ListByOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Set<ReturnCase>()
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }

    public async Task AddAsync(ReturnCase returnCase, CancellationToken cancellationToken = default)
    {
        dbContext.Set<ReturnCase>().Add(returnCase);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(ReturnCase returnCase, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
