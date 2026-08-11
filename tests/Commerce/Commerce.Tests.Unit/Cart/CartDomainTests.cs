using Commerce.Cart.Domain.Entities;
using Commerce.Cart.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Cart;

public sealed class CartDomainTests
{
    [Fact]
    public void AddOrIncreaseItem_AccumulatesQuantityForSameOffer()
    {
        var cart = ShoppingCart.CreateForGuest(1, "guest-token", 1, "IRR", DateTime.UtcNow.AddDays(1));

        cart.AddOrIncreaseItem(10, 2, maxItemQuantity: 100, maxDistinctItems: 50);
        cart.AddOrIncreaseItem(10, 3, maxItemQuantity: 100, maxDistinctItems: 50);

        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items.First().Quantity);
    }

    [Fact]
    public void UpdateItemQuantity_RejectsZero()
    {
        var cart = ShoppingCart.CreateForGuest(1, "guest-token", 1, "IRR", DateTime.UtcNow.AddDays(1));
        var item = cart.AddOrIncreaseItem(10, 1, 100, 50);

        Assert.Throws<ArgumentOutOfRangeException>(() => cart.UpdateItemQuantity(item.Id, 0, 100));
    }

    [Fact]
    public void EnsureModifiable_RejectsConvertedCart()
    {
        var cart = ShoppingCart.CreateForCustomer(1, 5, 1, "IRR", DateTime.UtcNow.AddDays(1));
        cart.MarkConverted();

        Assert.Throws<InvalidOperationException>(() => cart.AddOrIncreaseItem(10, 1, 100, 50));
    }

    [Fact]
    public void CreateForGuest_RequiresToken()
    {
        Assert.Throws<ArgumentException>(() =>
            ShoppingCart.CreateForGuest(1, " ", 1, "IRR", DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void CartItem_Create_RejectsInvalidQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CartItem.Create(1, 1, 0, 100));
    }
}
