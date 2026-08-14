using Commerce.Customers.Domain.Entities;

namespace Commerce.Customers.Application.Abstractions;

public interface IAffiliateRepository
{
    Task<Affiliate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Affiliate?> GetByReferralCodeAsync(string referralCode, int storeId, CancellationToken cancellationToken = default);

    Task<Affiliate?> GetByCustomerIdAsync(int customerId, int storeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Affiliate>> ListAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddAsync(Affiliate affiliate, CancellationToken cancellationToken = default);

    Task UpdateAsync(Affiliate affiliate, CancellationToken cancellationToken = default);

    Task<AffiliateCommissionAccount?> GetCommissionAccountWithTransactionsAsync(
        int affiliateId,
        string currencyCode,
        CancellationToken cancellationToken = default);

    Task AddCommissionAccountAsync(AffiliateCommissionAccount account, CancellationToken cancellationToken = default);

    Task UpdateCommissionAccountAsync(AffiliateCommissionAccount account, CancellationToken cancellationToken = default);

    Task<AffiliateReferral?> GetReferralAsync(int affiliateId, int referredCustomerId, CancellationToken cancellationToken = default);

    Task AddReferralAsync(AffiliateReferral referral, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AffiliateReferral>> ListReferralsAsync(int affiliateId, CancellationToken cancellationToken = default);
}
