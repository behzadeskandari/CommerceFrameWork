using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Customers.Infrastructure.Persistence.Repositories;

public sealed class EfAffiliateRepository(CommerceDbContext dbContext) : IAffiliateRepository
{
    public Task<Affiliate?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Affiliate>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<Affiliate?> GetByReferralCodeAsync(string referralCode, int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Affiliate>()
            .FirstOrDefaultAsync(x => x.ReferralCode == referralCode && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

    public Task<Affiliate?> GetByCustomerIdAsync(int customerId, int storeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Affiliate>()
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.StoreId == storeId && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Affiliate>> ListAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Affiliate>().AsNoTracking().Where(x => !x.IsDeleted);
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(Affiliate affiliate, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Affiliate>().Add(affiliate);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Affiliate affiliate, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Affiliate>().Update(affiliate);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<AffiliateCommissionAccount?> GetCommissionAccountWithTransactionsAsync(
        int affiliateId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<AffiliateCommissionAccount>()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(
                x => x.AffiliateId == affiliateId &&
                     x.CurrencyCode == currencyCode.Trim().ToUpperInvariant(),
                cancellationToken);

    public async Task AddCommissionAccountAsync(AffiliateCommissionAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.Set<AffiliateCommissionAccount>().Add(account);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateCommissionAccountAsync(AffiliateCommissionAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.Set<AffiliateCommissionAccount>().Update(account);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<AffiliateReferral?> GetReferralAsync(
        int affiliateId,
        int referredCustomerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<AffiliateReferral>()
            .FirstOrDefaultAsync(
                x => x.AffiliateId == affiliateId && x.ReferredCustomerId == referredCustomerId,
                cancellationToken);

    public async Task AddReferralAsync(AffiliateReferral referral, CancellationToken cancellationToken = default)
    {
        dbContext.Set<AffiliateReferral>().Add(referral);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AffiliateReferral>> ListReferralsAsync(
        int affiliateId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<AffiliateReferral>()
            .AsNoTracking()
            .Where(x => x.AffiliateId == affiliateId)
            .OrderByDescending(x => x.ReferredAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
