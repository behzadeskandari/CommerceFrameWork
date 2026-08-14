using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Promotions.Infrastructure.Persistence.Repositories;

public sealed class EfPromotionsRepository(CommerceDbContext dbContext) : IPromotionsRepository
{
    public Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(int storeId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        dbContext.Set<Promotion>()
            .Include("_conditions")
            .Include("_actions")
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => !x.StoreId.HasValue || x.StoreId.Value == storeId)
            .Where(x => (!x.StartsAtUtc.HasValue || utcNow >= x.StartsAtUtc.Value) &&
                        (!x.EndsAtUtc.HasValue || utcNow <= x.EndsAtUtc.Value))
            .OrderByDescending(x => x.Priority)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Promotion>)t.Result, cancellationToken);

    public Task<Promotion?> GetPromotionByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Promotion>()
            .Include("_conditions")
            .Include("_actions")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<IReadOnlyList<Promotion>> ListPromotionsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Promotion>()
            .Include("_conditions")
            .Include("_actions")
            .Where(x => !x.IsDeleted);

        if (storeId.HasValue)
        {
            query = query.Where(x => !x.StoreId.HasValue || x.StoreId.Value == storeId.Value);
        }

        return query.OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Promotion>)t.Result, cancellationToken);
    }

    public async Task AddPromotionAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Promotion>().Add(promotion);
        await Task.CompletedTask;
    }

    public async Task SavePromotionAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Promotion>().Update(promotion);
        await Task.CompletedTask;
    }

    public async Task DeletePromotionAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Promotion>().Update(promotion);
        await Task.CompletedTask;
    }

    public Task<int> GetCustomerUsageCountAsync(int promotionId, int customerId, CancellationToken cancellationToken = default) =>
        dbContext.Set<PromotionUsage>()
            .CountAsync(x => x.PromotionId == promotionId && x.CustomerId == customerId, cancellationToken);

    public async Task AddUsageAsync(PromotionUsage usage, CancellationToken cancellationToken = default)
    {
        dbContext.Set<PromotionUsage>().Add(usage);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
