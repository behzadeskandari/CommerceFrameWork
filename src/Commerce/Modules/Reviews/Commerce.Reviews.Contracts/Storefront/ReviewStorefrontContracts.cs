using Commerce.Framework.Core.Results;
using Commerce.Reviews.Domain.Enums;

namespace Commerce.Reviews.Contracts.Storefront;

public sealed record ProductRatingSummaryDto(
    double AverageRating,
    int RatingCount,
    IReadOnlyDictionary<int, int> Distribution);

public sealed record ProductReviewDto(
    int Id,
    int ProductId,
    int CustomerId,
    int Rating,
    string Title,
    string Content,
    ReviewModerationStatus ModerationStatus,
    bool IsVerifiedPurchase,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ProductReviewsPageDto(
    IReadOnlyList<ProductReviewDto> Reviews,
    ProductRatingSummaryDto Summary,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record SubmitProductReviewRequest(
    int Rating,
    string Title,
    string Content);

public sealed record UpdateProductReviewRequest(
    int Rating,
    string Title,
    string Content);

public interface IReviewStorefrontService
{
    Task<Result<ProductReviewsPageDto>> ListApprovedAsync(
        int productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<ProductRatingSummaryDto>> GetRatingSummaryAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<Result<ProductReviewDto>> GetOwnReviewAsync(
        int customerId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<Result<ProductReviewDto>> SubmitAsync(
        int customerId,
        int productId,
        SubmitProductReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProductReviewDto>> UpdateOwnAsync(
        int customerId,
        int reviewId,
        UpdateProductReviewRequest request,
        CancellationToken cancellationToken = default);
}
