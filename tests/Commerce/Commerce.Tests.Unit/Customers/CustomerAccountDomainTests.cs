using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Customers;

public sealed class CustomerAccountDomainTests
{
    [Fact]
    public void LoyaltyAccount_Earn_IncreasesBalance()
    {
        var account = LoyaltyAccount.Create(1, 1);

        account.PostTransaction(
            LoyaltyTransactionType.Earn,
            100,
            "earn-1",
            CustomerAccountReferenceType.Order,
            10,
            "Order points");

        Assert.Equal(100, account.PointsBalance);
        Assert.Single(account.Transactions);
    }

    [Fact]
    public void LoyaltyAccount_Spend_DecreasesBalance()
    {
        var account = LoyaltyAccount.Create(1, 1);
        account.PostTransaction(LoyaltyTransactionType.Earn, 100, "earn-1", CustomerAccountReferenceType.Order, 10, null);

        account.PostTransaction(
            LoyaltyTransactionType.Spend,
            -40,
            "spend-1",
            CustomerAccountReferenceType.Reward,
            5,
            "Reward");

        Assert.Equal(60, account.PointsBalance);
    }

    [Fact]
    public void LoyaltyAccount_DuplicateIdempotencyKey_ReturnsSameTransaction()
    {
        var account = LoyaltyAccount.Create(1, 1);

        var first = account.PostTransaction(
            LoyaltyTransactionType.Earn,
            50,
            "dup-key",
            CustomerAccountReferenceType.Manual,
            null,
            null);

        var second = account.PostTransaction(
            LoyaltyTransactionType.Earn,
            50,
            "dup-key",
            CustomerAccountReferenceType.Manual,
            null,
            null);

        Assert.Same(first, second);
        Assert.Equal(50, account.PointsBalance);
        Assert.Single(account.Transactions);
    }

    [Fact]
    public void LoyaltyAccount_Spend_RejectsInsufficientBalance()
    {
        var account = LoyaltyAccount.Create(1, 1);
        account.PostTransaction(LoyaltyTransactionType.Earn, 10, "earn-1", CustomerAccountReferenceType.Manual, null, null);

        Assert.Throws<InvalidOperationException>(() =>
            account.PostTransaction(
                LoyaltyTransactionType.Spend,
                -20,
                "spend-fail",
                CustomerAccountReferenceType.Reward,
                1,
                null));
    }

    [Fact]
    public void LoyaltyAccount_Expire_ReducesBalanceForExpiredEarn()
    {
        var account = LoyaltyAccount.Create(1, 1);
        var expiredAt = DateTime.UtcNow.AddDays(-1);
        account.PostTransaction(
            LoyaltyTransactionType.Earn,
            30,
            "earn-exp",
            CustomerAccountReferenceType.Order,
            1,
            null,
            expiredAt);

        var expirable = account.GetExpirablePoints(DateTime.UtcNow);
        Assert.Equal(30, expirable);

        account.PostTransaction(
            LoyaltyTransactionType.Expire,
            -30,
            "expire-earn-exp",
            CustomerAccountReferenceType.None,
            null,
            "Expired");

        account.Transactions.First(x => x.IdempotencyKey == "earn-exp").MarkExpired();
        Assert.Equal(0, account.PointsBalance);
    }

    [Fact]
    public void StoreCreditAccount_CreditAndDebit_MaintainsBalance()
    {
        var account = StoreCreditAccount.Create(1, 1, "USD");

        account.PostTransaction(
            StoreCreditTransactionType.Credit,
            25m,
            "credit-1",
            CustomerAccountReferenceType.Manual,
            null,
            "Manual credit");

        account.PostTransaction(
            StoreCreditTransactionType.Debit,
            -10m,
            "debit-1",
            CustomerAccountReferenceType.Order,
            99,
            "Applied to order");

        Assert.Equal(15m, account.Balance);
        Assert.Equal(2, account.Transactions.Count);
    }

    [Fact]
    public void StoreCreditAccount_DuplicateIdempotencyKey_IsIdempotent()
    {
        var account = StoreCreditAccount.Create(1, 1, "USD");

        account.PostTransaction(StoreCreditTransactionType.Credit, 20m, "key-1", CustomerAccountReferenceType.Manual, null, null);
        account.PostTransaction(StoreCreditTransactionType.Credit, 20m, "key-1", CustomerAccountReferenceType.Manual, null, null);

        Assert.Equal(20m, account.Balance);
        Assert.Single(account.Transactions);
    }

    [Fact]
    public void StoreCreditAccount_Debit_RejectsInsufficientBalance()
    {
        var account = StoreCreditAccount.Create(1, 1, "USD");

        Assert.Throws<InvalidOperationException>(() =>
            account.PostTransaction(
                StoreCreditTransactionType.Debit,
                -5m,
                "debit-fail",
                CustomerAccountReferenceType.Order,
                1,
                null));
    }

    [Fact]
    public void CustomerSegment_Create_RequiresRules()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CustomerSegment.Create(1, "VIP", null, []));
    }

    [Fact]
    public void LoyaltyRewardRedemption_Create_RequiresIdempotencyKey()
    {
        Assert.Throws<ArgumentException>(() =>
            LoyaltyRewardRedemption.Create(1, 1, 1, 50, " "));
    }
}
