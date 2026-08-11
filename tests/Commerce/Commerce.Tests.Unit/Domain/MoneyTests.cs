using Commerce.Framework.Domain.ValueObjects;
using Xunit;

namespace Commerce.Tests.Unit.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_Succeeds()
    {
        var money = Money.Create(10.50m, Currency.Usd);

        Assert.Equal(10.50m, money.Amount);
        Assert.Equal("USD", money.Currency.Code);
    }

    [Fact]
    public void Equality_ComparesAmountAndCurrency()
    {
        var left = Money.Create(1m, Currency.Eur);
        var right = Money.Create(1m, Currency.Eur);

        Assert.Equal(left, right);
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        var left = Money.Create(1.25m, Currency.Usd);
        var right = Money.Create(2.75m, Currency.Usd);

        var total = left.Add(right);

        Assert.Equal(4m, total.Amount);
        Assert.Equal(Currency.Usd, total.Currency);
    }

    [Fact]
    public void Subtract_WithSameCurrency_ReturnsDifference()
    {
        var left = Money.Create(5m, Currency.Usd);
        var right = Money.Create(2m, Currency.Usd);

        var result = left.Subtract(right);

        Assert.Equal(3m, result.Amount);
    }

    [Fact]
    public void Multiply_ScalesAmount()
    {
        var money = Money.Create(2m, Currency.Usd);

        var result = money.Multiply(3m);

        Assert.Equal(6m, result.Amount);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_Throws()
    {
        var usd = Money.Create(1m, Currency.Usd);
        var eur = Money.Create(1m, Currency.Eur);

        Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(-1m, Currency.Usd));
    }

    [Fact]
    public void Create_WithExcessivePrecision_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(1.12345m, Currency.Usd));
    }

    [Fact]
    public void Multiply_WithNegativeFactor_Throws()
    {
        var money = Money.Create(1m, Currency.Usd);

        Assert.Throws<ArgumentOutOfRangeException>(() => money.Multiply(-1m));
    }
}
