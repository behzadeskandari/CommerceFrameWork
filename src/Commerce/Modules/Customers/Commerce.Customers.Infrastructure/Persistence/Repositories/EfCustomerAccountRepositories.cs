using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Customers.Infrastructure.Persistence.Repositories;

public sealed class EfCustomerPreferenceRepository(CommerceDbContext dbContext) : ICustomerPreferenceRepository
{
    public async Task<IReadOnlyList<CustomerPreference>> ListByCustomerAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<CustomerPreference>().AsNoTracking().Where(x => x.CustomerId == customerId);
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId || x.StoreId == null);
        }

        return await query.OrderBy(x => x.PreferenceKey).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<CustomerPreference?> GetByKeyAsync(
        int customerId,
        int? storeId,
        string preferenceKey,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<CustomerPreference>()
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.StoreId == storeId && x.PreferenceKey == preferenceKey,
                cancellationToken);

    public async Task AddAsync(CustomerPreference preference, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerPreference>().Add(preference);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerPreference preference, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class EfCustomerSegmentRepository(CommerceDbContext dbContext) : ICustomerSegmentRepository
{
    public Task<CustomerSegment?> GetByIdWithRulesAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<CustomerSegment>().Include(x => x.Rules).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerSegment>> ListAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<CustomerSegment>().AsNoTracking().Include(x => x.Rules).AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(CustomerSegment segment, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerSegment>().Add(segment);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerSegment segment, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(CustomerSegment segment, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerSegment>().Remove(segment);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomerSegmentMembership>> ListMembershipsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<CustomerSegmentMembership>()
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.StoreId == storeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddMembershipAsync(CustomerSegmentMembership membership, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerSegmentMembership>().Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveMembershipsForCustomerAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await dbContext.Set<CustomerSegmentMembership>()
            .Where(x => x.CustomerId == customerId && x.StoreId == storeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (memberships.Count == 0)
        {
            return;
        }

        dbContext.Set<CustomerSegmentMembership>().RemoveRange(memberships);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class EfLoyaltyRepository(CommerceDbContext dbContext) : ILoyaltyRepository
{
    public Task<LoyaltyAccount?> GetAccountWithTransactionsAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<LoyaltyAccount>()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.StoreId == storeId, cancellationToken);

    public Task<LoyaltyAccount?> GetAccountByIdWithTransactionsAsync(
        int accountId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<LoyaltyAccount>()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);

    public async Task AddAccountAsync(LoyaltyAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.Set<LoyaltyAccount>().Add(account);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAccountAsync(LoyaltyAccount account, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<LoyaltyReward?> GetRewardByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<LoyaltyReward>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LoyaltyReward>> ListRewardsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<LoyaltyReward>().AsNoTracking().AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRewardAsync(LoyaltyReward reward, CancellationToken cancellationToken = default)
    {
        dbContext.Set<LoyaltyReward>().Add(reward);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateRewardAsync(LoyaltyReward reward, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRewardAsync(LoyaltyReward reward, CancellationToken cancellationToken = default)
    {
        dbContext.Set<LoyaltyReward>().Remove(reward);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<LoyaltyRewardRedemption?> GetRedemptionByIdempotencyKeyAsync(
        int customerId,
        int storeId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<LoyaltyRewardRedemption>()
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.StoreId == storeId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public async Task AddRedemptionAsync(LoyaltyRewardRedemption redemption, CancellationToken cancellationToken = default)
    {
        dbContext.Set<LoyaltyRewardRedemption>().Add(redemption);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateRedemptionAsync(LoyaltyRewardRedemption redemption, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class EfStoreCreditRepository(CommerceDbContext dbContext) : IStoreCreditRepository
{
    public Task<StoreCreditAccount?> GetAccountWithTransactionsAsync(
        int customerId,
        int storeId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<StoreCreditAccount>()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.StoreId == storeId && x.CurrencyCode == currencyCode.ToUpperInvariant(),
                cancellationToken);

    public async Task AddAccountAsync(StoreCreditAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.Set<StoreCreditAccount>().Add(account);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAccountAsync(StoreCreditAccount account, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class EfCustomerActivityRepository(CommerceDbContext dbContext) : ICustomerActivityRepository
{
    public async Task<IReadOnlyList<CustomerActivityLog>> ListAsync(
        int customerId,
        int? storeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<CustomerActivityLog>().AsNoTracking().Where(x => x.CustomerId == customerId);
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId || x.StoreId == null);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(CustomerActivityLog activity, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerActivityLog>().Add(activity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
