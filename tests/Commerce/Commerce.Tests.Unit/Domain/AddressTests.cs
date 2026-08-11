using Commerce.Framework.Domain.ValueObjects;
using Xunit;

namespace Commerce.Tests.Unit.Domain;

public sealed class AddressTests
{
    [Fact]
    public void Create_WithRequiredFields_Succeeds()
    {
        var address = Address.Create(
            firstName: "John",
            lastName: "Doe",
            country: "United States",
            city: "Seattle",
            address1: "123 Main St",
            postalCode: "98101",
            stateProvince: "WA",
            phoneNumber: "+1-555-0100");

        Assert.Equal("John", address.FirstName);
        Assert.Equal("Doe", address.LastName);
        Assert.Equal("John Doe", address.FullName);
        Assert.Equal("98101", address.PostalCode);
    }

    [Fact]
    public void Create_WithMissingCity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Address.Create("John", "Doe", "US", " ", "123 Main", "98101"));
    }

    [Fact]
    public void Equality_ComparesNormalizedValues()
    {
        var first = Address.Create("John", "Doe", "US", "Seattle", "123 Main", "98101");
        var second = Address.Create("John", "Doe", "US", "Seattle", "123 Main", "98101");

        Assert.Equal(first, second);
    }
}
