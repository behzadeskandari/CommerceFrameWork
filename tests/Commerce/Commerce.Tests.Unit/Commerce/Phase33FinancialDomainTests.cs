using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Payments.Domain.Entities;
using Commerce.Payments.Domain.Enums;
using Xunit;

namespace CommerceTests.Unit.Phase33Financial;

public sealed class Phase33FinancialDomainTests
{
    [Fact]
    public void GiftCard_Redeem_DecreasesBalance()
    {
        var card = GiftCard.Create("GC-TEST", 1, "USD", 100m, true, null, null, null, null, null);

        card.PostTransaction(
            GiftCardTransactionType.Redeem,
            -40m,
            "redeem-1",
            GiftCardReferenceType.Order,
            10,
            "Checkout");

        Assert.Equal(60m, card.Balance);
    }

    [Fact]
    public void GiftCard_DuplicateIdempotencyKey_DoesNotDoubleSpend()
    {
        var card = GiftCard.Create("GC-DUP", 1, "USD", 50m, true, null, null, null, null, null);

        card.PostTransaction(GiftCardTransactionType.Redeem, -20m, "dup-key", GiftCardReferenceType.Order, 1, null);
        card.PostTransaction(GiftCardTransactionType.Redeem, -20m, "dup-key", GiftCardReferenceType.Order, 1, null);

        Assert.Equal(30m, card.Balance);
        Assert.Single(card.Transactions.Where(x => x.Type == GiftCardTransactionType.Redeem));
    }

    [Fact]
    public void GiftCard_Redeem_RejectsInsufficientBalance()
    {
        var card = GiftCard.Create("GC-LOW", 1, "USD", 10m, true, null, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() =>
            card.PostTransaction(GiftCardTransactionType.Redeem, -20m, "fail", GiftCardReferenceType.Order, 1, null));
    }

    [Fact]
    public void GiftCard_Expired_IsNotCurrentlyValid()
    {
        var card = GiftCard.Create(
            "GC-EXP",
            1,
            "USD",
            25m,
            true,
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            null,
            null);

        Assert.False(card.IsCurrentlyValid(DateTime.UtcNow));
    }

    [Fact]
    public void AffiliateCommission_Earn_IncreasesBalance()
    {
        var account = AffiliateCommissionAccount.Create(1, 1, "USD");

        account.PostTransaction(
            AffiliateCommissionTransactionType.Earn,
            15m,
            "earn-1",
            AffiliateCommissionReferenceType.Order,
            100,
            "Order commission");

        Assert.Equal(15m, account.Balance);
    }

    [Fact]
    public void AffiliateCommission_DuplicateIdempotencyKey_IsIdempotent()
    {
        var account = AffiliateCommissionAccount.Create(1, 1, "USD");

        account.PostTransaction(
            AffiliateCommissionTransactionType.Earn,
            10m,
            "dup",
            AffiliateCommissionReferenceType.Order,
            1,
            null);

        account.PostTransaction(
            AffiliateCommissionTransactionType.Earn,
            10m,
            "dup",
            AffiliateCommissionReferenceType.Order,
            1,
            null);

        Assert.Equal(10m, account.Balance);
        Assert.Single(account.Transactions);
    }

    [Fact]
    public void AffiliateCommission_Payout_RejectsNegativeBalance()
    {
        var account = AffiliateCommissionAccount.Create(1, 1, "USD");
        account.PostTransaction(
            AffiliateCommissionTransactionType.Earn,
            5m,
            "earn",
            AffiliateCommissionReferenceType.Order,
            1,
            null);

        Assert.Throws<InvalidOperationException>(() =>
            account.PostTransaction(
                AffiliateCommissionTransactionType.Payout,
                -10m,
                "payout",
                AffiliateCommissionReferenceType.Payout,
                null,
                null));
    }

    [Fact]
    public void Affiliate_ReferralCode_IsNormalized()
    {
        var affiliate = Affiliate.Create(1, 1, " ref-code ", 10m, true);
        Assert.Equal("REF-CODE", affiliate.ReferralCode);
    }
}
