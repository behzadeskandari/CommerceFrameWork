using Commerce.Catalog.Contracts.Products;
using Commerce.Downloads.Domain.Entities;

namespace Commerce.Tests.Unit.Downloads;

public sealed class DigitalProductTypesTests
{
    [Theory]
    [InlineData("Digital", true)]
    [InlineData("Downloadable", true)]
    [InlineData("Virtual", true)]
    [InlineData("Simple", false)]
    [InlineData("Variant", false)]
    public void IsDigital_ClassifiesProductTypes(string productType, bool expected) =>
        Assert.Equal(expected, DigitalProductTypes.IsDigital(productType));
}

public sealed class DownloadEntitlementDomainTests
{
    [Fact]
    public void Grant_AllowsDownloadsUntilLimitReached()
    {
        var entitlement = DownloadEntitlement.Grant(
            1, 2, 3, 1, 10, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 2);

        entitlement.RecordSuccessfulDownload(DateTime.UtcNow);
        Assert.True(entitlement.HasRemainingDownloads());

        entitlement.RecordSuccessfulDownload(DateTime.UtcNow);
        Assert.False(entitlement.HasRemainingDownloads());
    }

    [Fact]
    public void Grant_UnlimitedDownloads_NeverBlocks()
    {
        var entitlement = DownloadEntitlement.Grant(
            1, 2, 3, 1, 10, null, DateTime.UtcNow, null, null);

        for (var i = 0; i < 100; i++)
        {
            entitlement.RecordSuccessfulDownload(DateTime.UtcNow);
        }

        Assert.True(entitlement.HasRemainingDownloads());
    }

    [Fact]
    public void IsExpired_ReturnsTrueAfterExpiration()
    {
        var entitlement = DownloadEntitlement.Grant(
            1, 2, 3, 1, 10, null, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), null);

        Assert.True(entitlement.IsExpired(DateTime.UtcNow));
    }

    [Fact]
    public void IsOwnedByCustomer_ValidatesCustomer()
    {
        var entitlement = DownloadEntitlement.Grant(
            1, 2, 3, 1, 10, null, DateTime.UtcNow, null, null);

        Assert.True(entitlement.IsOwnedByCustomer(10));
        Assert.False(entitlement.IsOwnedByCustomer(11));
    }

    [Fact]
    public void RecordSuccessfulDownload_ThrowsWhenExpired()
    {
        var entitlement = DownloadEntitlement.Grant(
            1, 2, 3, 1, 10, null, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), 5);

        Assert.Throws<InvalidOperationException>(() => entitlement.RecordSuccessfulDownload(DateTime.UtcNow));
    }
}

public sealed class ProductDownloadSettingsTests
{
    [Fact]
    public void Create_CalculatesExpirationFromDays()
    {
        var grantedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var settings = ProductDownloadSettings.Create(1, 1, true, 3, 30);

        var expires = settings.CalculateExpirationUtc(grantedAt);
        Assert.Equal(grantedAt.AddDays(30), expires);
    }
}

public sealed class DownloadStorageKeyValidationTests
{
    [Theory]
    [InlineData("../secret.txt", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("downloads/product/file.zip", true)]
    [InlineData("", false)]
    public void IsValidStorageKey_BlocksTraversal(string key, bool expected) =>
        Assert.Equal(expected, Commerce.Downloads.Application.Storefront.CustomerDownloadService.IsValidStorageKey(key));
}

public sealed class CheckoutRequiresShippingDigitalTests
{
    [Fact]
    public void ShippingCalculationService_SkipsDigitalLines()
    {
        Assert.True(Commerce.Shipping.Application.Shipping.ShippingCalculationService.IsNonShippableProductType("Digital"));
        Assert.False(Commerce.Shipping.Application.Shipping.ShippingCalculationService.IsNonShippableProductType("Simple"));
    }

    [Fact]
    public async Task CheckoutRequiresShipping_DigitalOnlyOrder_DoesNotRequireShipping()
    {
        var reader = new FakeProductReader(new Dictionary<int, string>
        {
            [1] = "Digital",
            [2] = "Virtual"
        });
        var evaluator = new Commerce.Checkout.Application.Checkout.CheckoutRequiresShippingEvaluator(reader);

        var requiresShipping = await evaluator.RequiresShippingAsync([1, 2]);

        Assert.False(requiresShipping);
    }

    [Fact]
    public async Task CheckoutRequiresShipping_MixedOrder_RequiresShipping()
    {
        var reader = new FakeProductReader(new Dictionary<int, string>
        {
            [1] = "Digital",
            [2] = "Simple"
        });
        var evaluator = new Commerce.Checkout.Application.Checkout.CheckoutRequiresShippingEvaluator(reader);

        var requiresShipping = await evaluator.RequiresShippingAsync([1, 2]);

        Assert.True(requiresShipping);
    }

    [Fact]
    public async Task CheckoutRequiresShipping_PhysicalOnly_RequiresShipping()
    {
        var reader = new FakeProductReader(new Dictionary<int, string> { [1] = "Simple" });
        var evaluator = new Commerce.Checkout.Application.Checkout.CheckoutRequiresShippingEvaluator(reader);

        Assert.True(await evaluator.RequiresShippingAsync([1]));
    }
}

public sealed class DownloadHistoryEntryTests
{
    [Fact]
    public void Record_FailedAttempt_StoresReason()
    {
        var entry = DownloadHistoryEntry.Record(1, 2, 10, DateTime.UtcNow, false, "127.0.0.1", "TestAgent", "Unauthorized.");

        Assert.False(entry.WasSuccessful);
        Assert.Equal("Unauthorized.", entry.FailureReason);
        Assert.Equal("127.0.0.1", entry.IpAddress);
    }

    [Fact]
    public void Record_SuccessfulAttempt_OmitsFailureReason()
    {
        var entry = DownloadHistoryEntry.Record(1, 2, 10, DateTime.UtcNow, true);

        Assert.True(entry.WasSuccessful);
        Assert.Null(entry.FailureReason);
    }
}

public sealed class DownloadEntitlementRevocationTests
{
    [Fact]
    public void RevokedEntitlement_BlocksDownload()
    {
        var entitlement = DownloadEntitlement.Grant(
            1, 2, 3, 1, 10, null, DateTime.UtcNow, null, null);
        entitlement.Revoke();

        Assert.Throws<InvalidOperationException>(() => entitlement.RecordSuccessfulDownload(DateTime.UtcNow));
    }

    [Fact]
    public void GuestAccess_ValidatesToken()
    {
        var entitlement = DownloadEntitlement.Grant(
            1, 2, 3, 1, null, "guest-token", DateTime.UtcNow, null, null);

        Assert.True(entitlement.IsAccessibleByGuest("guest-token"));
        Assert.False(entitlement.IsAccessibleByGuest("wrong-token"));
    }
}

file sealed class FakeProductReader(Dictionary<int, string> productTypes) : global::Commerce.Catalog.Contracts.Products.IProductReader
{
    public Task<global::Commerce.Framework.Core.Results.Result<global::Commerce.Catalog.Contracts.Products.ProductDetailDto>> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (!productTypes.TryGetValue(productId, out var productType))
        {
            return Task.FromResult(global::Commerce.Framework.Core.Results.Result.Failure<global::Commerce.Catalog.Contracts.Products.ProductDetailDto>(
                global::Commerce.Framework.Core.Errors.Error.NotFound("Product not found.")));
        }

        var dto = new global::Commerce.Catalog.Contracts.Products.ProductDetailDto(
            productId,
            $"Product {productId}",
            null,
            null,
            $"SKU-{productId}",
            productType,
            true,
            true,
            true,
            false,
            0,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            0,
            null,
            [],
            []);

        return Task.FromResult(global::Commerce.Framework.Core.Results.Result.Success(dto));
    }

    public Task<global::Commerce.Framework.Core.Results.Result<IReadOnlyList<global::Commerce.Catalog.Contracts.Products.ProductSummaryDto>>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(global::Commerce.Framework.Core.Results.Result.Success<IReadOnlyList<global::Commerce.Catalog.Contracts.Products.ProductSummaryDto>>([]));
}
