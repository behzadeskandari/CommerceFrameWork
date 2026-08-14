using Commerce.Reviews.Domain.Entities;
using Commerce.Reviews.Domain.Enums;

namespace Commerce.Reviews.Application.Abstractions;

public sealed record ReviewListCriteria(
    int? StoreId = null,
    int? ProductId = null,
    int? CustomerId = null,
    ReviewModerationStatus? ModerationStatus = null,
    bool ApprovedOnly = false,
    int Page = 1,
    int PageSize = 20);

public sealed record WishlistListCriteria(
    int? StoreId = null,
    int? CustomerId = null,
    int Page = 1,
    int PageSize = 20);

public sealed record ProductRatingAggregate(
    double AverageRating,
    int RatingCount,
    IReadOnlyDictionary<int, int> Distribution);

public interface IReviewsRepository
{
    Task<ProductReview?> GetReviewByIdAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<ProductReview?> GetReviewByProductAndCustomerAsync(
        int productId,
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default);

    Task SaveReviewAsync(ProductReview review, CancellationToken cancellationToken = default);

    Task DeleteReviewAsync(ProductReview review, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductReview> Items, int TotalCount)> ListReviewsAsync(
        ReviewListCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<ProductRatingAggregate> GetRatingAggregateAsync(
        int productId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Wishlist?> GetWishlistByCustomerAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task<Wishlist?> GetWishlistByIdAsync(int wishlistId, CancellationToken cancellationToken = default);

    Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default);

    Task SaveWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Wishlist> Items, int TotalCount)> ListWishlistsAsync(
        WishlistListCriteria criteria,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
