using Commerce.Customers.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Customers.Infrastructure.Persistence;

public sealed class CustomersModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new CustomerCustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerAddressConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerPreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerSegmentConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerSegmentRuleConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerSegmentMembershipConfiguration());
        modelBuilder.ApplyConfiguration(new LoyaltyAccountConfiguration());
        modelBuilder.ApplyConfiguration(new LoyaltyTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new LoyaltyRewardConfiguration());
        modelBuilder.ApplyConfiguration(new LoyaltyRewardRedemptionConfiguration());
        modelBuilder.ApplyConfiguration(new StoreCreditAccountConfiguration());
        modelBuilder.ApplyConfiguration(new StoreCreditTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerActivityLogConfiguration());
        modelBuilder.ApplyConfiguration(new AffiliateConfiguration());
        modelBuilder.ApplyConfiguration(new AffiliateReferralConfiguration());
        modelBuilder.ApplyConfiguration(new AffiliateCommissionAccountConfiguration());
        modelBuilder.ApplyConfiguration(new AffiliateCommissionTransactionConfiguration());
    }
}
