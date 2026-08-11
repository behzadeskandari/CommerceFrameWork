using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Catalog.Domain.Services;
using Commerce.Catalog.Domain.ValueObjects;
using Xunit;

namespace Commerce.Tests.Unit.Catalog;

public sealed class SkuTests
{
    [Fact]
    public void Create_NormalizesToUpperInvariant()
    {
        var sku = Sku.Create(" abc-123 ");
        Assert.Equal("ABC-123", sku.Value);
    }

    [Fact]
    public void Create_EmptySku_Throws()
    {
        Assert.Throws<ArgumentException>(() => Sku.Create(" "));
    }
}

public sealed class ProductTests
{
    [Fact]
    public void Create_SetsInitialState()
    {
        var product = Product.Create("Test Product", Sku.Create("SKU-1"), ProductType.Simple, published: true);
        Assert.Equal("Test Product", product.Name);
        Assert.True(product.Published);
        Assert.False(product.Deleted);
    }

    [Fact]
    public void SoftDelete_MarksDeletedAndUnpublished()
    {
        var product = Product.Create("Test", Sku.Create("SKU-2"), ProductType.Simple, published: true);
        product.SoftDelete();
        Assert.True(product.Deleted);
        Assert.False(product.Published);
    }
}

public sealed class CategoryHierarchyValidatorTests
{
    [Fact]
    public void WouldCreateCycle_SelfParent_ReturnsTrue()
    {
        var result = CategoryHierarchyValidator.WouldCreateCycle(1, 1, _ => null);
        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCycle_DirectCycle_ReturnsTrue()
    {
        var parents = new Dictionary<int, int?> { [2] = 1, [3] = 2 };
        var result = CategoryHierarchyValidator.WouldCreateCycle(1, 3, id => parents.GetValueOrDefault(id));
        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCycle_ValidParent_ReturnsFalse()
    {
        var parents = new Dictionary<int, int?> { [2] = 1 };
        var result = CategoryHierarchyValidator.WouldCreateCycle(3, 2, id => parents.GetValueOrDefault(id));
        Assert.False(result);
    }
}

public sealed class ProductAttributeTests
{
    [Fact]
    public void Definition_Create_NormalizesCode()
    {
        var definition = ProductAttributeDefinition.Create("Brand", " BRAND ");
        Assert.Equal("brand", definition.Code);
    }

    [Fact]
    public void Value_Create_RequiresValue()
    {
        Assert.Throws<ArgumentException>(() => ProductAttributeValue.Create(1, 1, " "));
    }
}
