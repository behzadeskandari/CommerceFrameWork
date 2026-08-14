using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Application.Storefront;
using Commerce.Reviews.Contracts.Storefront;
using Commerce.Reviews.Domain.Entities;
using Commerce.Reviews.Domain.Enums;

namespace Commerce.Tests.Unit.Reviews;

public sealed class ReviewStorefrontServiceTests
{
    private readonly FakeReviewsRepository _repository = new();
    private readonly FakeProductReader _productReader = new();
    private readonly FakePurchaseVerifier _purchaseVerifier = new();
    private readonly FakeStoreContext _storeContext = new(1);

    private ReviewStorefrontService CreateService() =>
        new(_repository, _productReader, _purchaseVerifier, _storeContext);

    [Fact]
    public async Task SubmitAsync_PreventsDuplicateReview()
    {
        _productReader.Product = SampleProduct();
        _repository.ExistingReview = ProductReview.Create(1, 10, 1, 5, "First", "Body", false, DateTime.UtcNow);

        var service = CreateService();
        var result = await service.SubmitAsync(10, 1, new SubmitProductReviewRequest(4, "Second", "Again"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task SubmitAsync_SetsVerifiedPurchaseFromOrders()
    {
        _productReader.Product = SampleProduct();
        _purchaseVerifier.HasPurchase = true;

        var service = CreateService();
        var result = await service.SubmitAsync(10, 1, new SubmitProductReviewRequest(5, "Verified", "Bought it"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsVerifiedPurchase);
        Assert.Equal(ReviewModerationStatus.Pending, result.Value.ModerationStatus);
    }

    [Fact]
    public async Task UpdateOwnAsync_ForbidsOtherCustomers()
    {
        var review = ProductReview.Create(1, 10, 1, 5, "Mine", "Body", false, DateTime.UtcNow);
        typeof(ProductReview).GetProperty(nameof(ProductReview.Id))!
            .SetValue(review, 42);
        _repository.Reviews[review.Id] = review;

        var service = CreateService();
        var result = await service.UpdateOwnAsync(11, 42, new UpdateProductReviewRequest(3, "Hack", "No"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task ListApprovedAsync_DoesNotReturnPendingReviews()
    {
        _productReader.Product = SampleProduct();
        var approved = ProductReview.Create(1, 10, 1, 5, "Public", "Visible", false, DateTime.UtcNow);
        approved.Approve(DateTime.UtcNow);
        var pending = ProductReview.Create(1, 11, 1, 1, "Hidden", "Wait", false, DateTime.UtcNow);
        _repository.Reviews[1] = approved;
        _repository.Reviews[2] = pending;

        var service = CreateService();
        var result = await service.ListApprovedAsync(1, 1, 10);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Reviews);
        Assert.Equal("Public", result.Value.Reviews[0].Title);
    }

    private static ProductDetailDto SampleProduct() =>
        new(1, "Widget", null, null, "SKU-1", "Simple", true, true, true, false, 0, "widget", DateTime.UtcNow, DateTime.UtcNow, 0, null, [], []);

    private sealed class FakeStoreContext(int storeId) : IStoreContext
    {
        public int? CurrentStoreId { get; } = storeId;
        public string? CurrentStoreSystemName => "default";
        public string? CurrentStoreName => "Default";
        public bool HasStore => true;
        public int? CurrentLanguageId => 1;
        public string? CurrentLanguageCode => "en";
        public string? CurrentCultureCode => "en-US";
        public bool IsRtl => false;
        public int? CurrentCurrencyId => 1;
        public string? CurrentCurrencyCode => "USD";
    }

    private sealed class FakeProductReader : IProductReader
    {
        public ProductDetailDto? Product { get; set; }

        public Task<Result<ProductDetailDto>> GetByIdAsync(int productId, CancellationToken cancellationToken = default) =>
            Product is null
                ? Task.FromResult(Result.Failure<ProductDetailDto>(Error.NotFound("Product not found.")))
                : Task.FromResult(Result.Success(Product));

        public Task<Result<IReadOnlyList<ProductSummaryDto>>> ListAsync(bool includeDeleted = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<IReadOnlyList<ProductSummaryDto>>([]));
    }

    private sealed class FakePurchaseVerifier : IOrderPurchaseVerifier
    {
        public bool HasPurchase { get; set; }

        public Task<bool> HasCustomerPurchasedProductAsync(int customerId, int productId, int storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(HasPurchase);
    }

    private sealed class FakeReviewsRepository : IReviewsRepository
    {
        public Dictionary<int, ProductReview> Reviews { get; } = [];
        public ProductReview? ExistingReview { get; set; }
        private int _nextId = 1;

        public Task<ProductReview?> GetReviewByIdAsync(int reviewId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Reviews.GetValueOrDefault(reviewId));

        public Task<ProductReview?> GetReviewByProductAndCustomerAsync(int productId, int customerId, int storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingReview);

        public Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default)
        {
            typeof(ProductReview).GetProperty(nameof(ProductReview.Id))!.SetValue(review, _nextId++);
            Reviews[review.Id] = review;
            return Task.CompletedTask;
        }

        public Task SaveReviewAsync(ProductReview review, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteReviewAsync(ProductReview review, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<ProductReview> Items, int TotalCount)> ListReviewsAsync(ReviewListCriteria criteria, CancellationToken cancellationToken = default)
        {
            IEnumerable<ProductReview> query = Reviews.Values;
            if (criteria.StoreId.HasValue)
            {
                query = query.Where(x => x.StoreId == criteria.StoreId.Value);
            }

            if (criteria.ProductId.HasValue)
            {
                query = query.Where(x => x.ProductId == criteria.ProductId.Value);
            }

            if (criteria.ModerationStatus.HasValue)
            {
                query = query.Where(x => x.ModerationStatus == criteria.ModerationStatus.Value);
            }
            else if (criteria.ApprovedOnly)
            {
                query = query.Where(x => x.ModerationStatus == ReviewModerationStatus.Approved);
            }

            var list = query.ToList();
            return Task.FromResult<(IReadOnlyList<ProductReview>, int)>((list, list.Count));
        }

        public Task<ProductRatingAggregate> GetRatingAggregateAsync(int productId, int storeId, CancellationToken cancellationToken = default)
        {
            var ratings = Reviews.Values
                .Where(x => x.ProductId == productId && x.StoreId == storeId && x.ModerationStatus == ReviewModerationStatus.Approved)
                .Select(x => x.Rating)
                .ToList();
            var rating = Commerce.Reviews.Application.Rating.ProductRatingCalculator.Compute(ratings);
            return Task.FromResult(new ProductRatingAggregate(rating.AverageRating, ratings.Count, rating.Distribution));
        }

        public Task<Wishlist?> GetWishlistByCustomerAsync(int customerId, int storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Wishlist?>(null);

        public Task<Wishlist?> GetWishlistByIdAsync(int wishlistId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Wishlist?>(null);

        public Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<Wishlist> Items, int TotalCount)> ListWishlistsAsync(WishlistListCriteria criteria, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Wishlist>, int)>(([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

public sealed class WishlistStorefrontServiceTests
{
    [Fact]
    public async Task RemoveItemAsync_RequiresOwnedWishlistItem()
    {
        var repository = new WishlistFakeRepository();
        var productReader = new SimpleProductReader(
            new ProductDetailDto(1, "Widget", null, null, "SKU", "Simple", true, true, true, false, 0, "widget", DateTime.UtcNow, DateTime.UtcNow, 0, null, [], []));
        var service = new WishlistStorefrontService(repository, productReader, new SimpleStoreContext(1));

        var result = await service.RemoveItemAsync(10, 999);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task AddItemAsync_EnforcesStoreScopedWishlist()
    {
        var repository = new WishlistFakeRepository();
        var productReader = new SimpleProductReader(
            new ProductDetailDto(5, "Gadget", null, null, "SKU-5", "Simple", true, true, true, false, 0, "gadget", DateTime.UtcNow, DateTime.UtcNow, 0, null, [], []));
        var service = new WishlistStorefrontService(repository, productReader, new SimpleStoreContext(2));

        var result = await service.AddItemAsync(10, new AddWishlistItemRequest(5));

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastSavedWishlist);
        Assert.Equal(2, repository.LastSavedWishlist!.StoreId);
        Assert.Equal(10, repository.LastSavedWishlist.CustomerId);
    }

    private sealed class SimpleStoreContext(int storeId) : IStoreContext
    {
        public int? CurrentStoreId { get; } = storeId;
        public string? CurrentStoreSystemName => "default";
        public string? CurrentStoreName => "Default";
        public bool HasStore => true;
        public int? CurrentLanguageId => 1;
        public string? CurrentLanguageCode => "en";
        public string? CurrentCultureCode => "en-US";
        public bool IsRtl => false;
        public int? CurrentCurrencyId => 1;
        public string? CurrentCurrencyCode => "USD";
    }

    private sealed class SimpleProductReader(ProductDetailDto product) : IProductReader
    {
        public Task<Result<ProductDetailDto>> GetByIdAsync(int productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(product));

        public Task<Result<IReadOnlyList<ProductSummaryDto>>> ListAsync(bool includeDeleted = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<IReadOnlyList<ProductSummaryDto>>([]));
    }

    private sealed class WishlistFakeRepository : IReviewsRepository
    {
        public Wishlist? LastSavedWishlist { get; private set; }

        public Task<Wishlist?> GetWishlistByCustomerAsync(int customerId, int storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Wishlist?>(null);

        public Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
        {
            typeof(Wishlist).GetProperty(nameof(Wishlist.Id))!.SetValue(wishlist, 1);
            LastSavedWishlist = wishlist;
            return Task.CompletedTask;
        }

        public Task SaveWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
        {
            LastSavedWishlist = wishlist;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ProductReview?> GetReviewByIdAsync(int reviewId, CancellationToken cancellationToken = default) => Task.FromResult<ProductReview?>(null);
        public Task<ProductReview?> GetReviewByProductAndCustomerAsync(int productId, int customerId, int storeId, CancellationToken cancellationToken = default) => Task.FromResult<ProductReview?>(null);
        public Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveReviewAsync(ProductReview review, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteReviewAsync(ProductReview review, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<ProductReview> Items, int TotalCount)> ListReviewsAsync(ReviewListCriteria criteria, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<ProductReview>, int)>(([], 0));
        public Task<ProductRatingAggregate> GetRatingAggregateAsync(int productId, int storeId, CancellationToken cancellationToken = default) => Task.FromResult(new ProductRatingAggregate(0, 0, new Dictionary<int, int>()));
        public Task<Wishlist?> GetWishlistByIdAsync(int wishlistId, CancellationToken cancellationToken = default) => Task.FromResult<Wishlist?>(null);
        public Task<(IReadOnlyList<Wishlist> Items, int TotalCount)> ListWishlistsAsync(WishlistListCriteria criteria, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Wishlist>, int)>(([], 0));
    }
}
