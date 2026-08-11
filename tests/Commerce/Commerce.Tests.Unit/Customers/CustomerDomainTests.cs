using Commerce.Customers.Domain.Entities;
using Xunit;

namespace Commerce.Tests.Unit.Customers;

public sealed class CustomerDomainTests
{
    [Fact]
    public void Create_NormalizesEmail()
    {
        var customer = Customer.Create("user-1", " Test@Example.com ", "Jane", "Doe");
        Assert.Equal("TEST@EXAMPLE.COM", customer.NormalizedEmail);
        Assert.Equal("Test@Example.com", customer.Email);
    }

    [Fact]
    public void Create_EmptyEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() => Customer.Create("user-1", " ", "Jane", "Doe"));
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var customer = Customer.Create("user-1", "a@test.com", "Jane", "Doe");
        customer.Deactivate();
        Assert.False(customer.Active);
    }

    [Fact]
    public void MarkDeleted_PreventsUpdates()
    {
        var customer = Customer.Create("user-1", "a@test.com", "Jane", "Doe");
        customer.MarkDeleted();
        Assert.Throws<InvalidOperationException>(() => customer.UpdateProfile("New", "Name", null));
    }
}

public sealed class CustomerAddressDomainTests
{
    [Fact]
    public void Create_StoresCustomerOwnership()
    {
        var address = Commerce.Framework.Domain.ValueObjects.Address.Create(
            "Jane",
            "Doe",
            "US",
            "Seattle",
            "123 Main",
            "98101");

        var entity = CustomerAddress.Create(42, "Home", address, isDefaultBilling: true);
        Assert.Equal(42, entity.CustomerId);
        Assert.True(entity.IsDefaultBilling);
    }

    [Fact]
    public void Create_EmptyLabel_Throws()
    {
        var address = Commerce.Framework.Domain.ValueObjects.Address.Create(
            "Jane",
            "Doe",
            "US",
            "Seattle",
            "123 Main",
            "98101");

        Assert.Throws<ArgumentException>(() => CustomerAddress.Create(1, " ", address));
    }
}
