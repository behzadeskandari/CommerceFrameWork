using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Contracts.Storefront;
using Commerce.Reviews.Domain.Entities;
using Commerce.Reviews.Domain.Enums;

namespace Commerce.Reviews.Application.Storefront;

public sealed class ReviewStorefrontService(
    IReviewsRepository reviewsRepository,
    IProductReader productReader,
    IOrderPurchaseVerifier purchaseVerifier,
    IStoreContext storeContext) : IReviewStorefrontService
{
    public async Task<Result<ProductReviewsPageDto>> ListApprovedAsync(
        int productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var productResult = await productReader.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (productResult.IsFailure)
        {
            return Result.Failure<ProductReviewsPageDto>(productResult.Error!);
        }

        var criteria = new ReviewListCriteria(
            storeId,
            ProductId: productId,
            ModerationStatus: ReviewModerationStatus.Approved,
            ApprovedOnly: true,
            Page: page,
            PageSize: pageSize);

        var (items, totalCount) = await reviewsRepository
            .ListReviewsAsync(criteria, cancellationToken)
            .ConfigureAwait(false);

        var summary = await BuildSummaryAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        var reviews = items.Select(MapReview).ToList();

        return Result.Success(new ProductReviewsPageDto(reviews, summary, totalCount, page, pageSize));
    }

    public async Task<Result<ProductRatingSummaryDto>> GetRatingSummaryAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var productResult = await productReader.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (productResult.IsFailure)
        {
            return Result.Failure<ProductRatingSummaryDto>(productResult.Error!);
        }

        var summary = await BuildSummaryAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success(summary);
    }

    public async Task<Result<ProductReviewDto>> GetOwnReviewAsync(
        int customerId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var review = await reviewsRepository
            .GetReviewByProductAndCustomerAsync(productId, customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (review is null)
        {
            return Result.Failure<ProductReviewDto>(Error.NotFound("Review not found."));
        }

        return Result.Success(MapReview(review));
    }

    public async Task<Result<ProductReviewDto>> SubmitAsync(
        int customerId,
        int productId,
        SubmitProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var productResult = await productReader.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (productResult.IsFailure)
        {
            return Result.Failure<ProductReviewDto>(productResult.Error!);
        }

        var existing = await reviewsRepository
            .GetReviewByProductAndCustomerAsync(productId, customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result.Failure<ProductReviewDto>(Error.Conflict("You have already reviewed this product."));
        }

        var isVerified = await purchaseVerifier
            .HasCustomerPurchasedProductAsync(customerId, productId, storeId, cancellationToken)
            .ConfigureAwait(false);

        ProductReview review;
        try
        {
            review = ProductReview.Create(
                productId,
                customerId,
                storeId,
                request.Rating,
                request.Title,
                request.Content,
                isVerified,
                DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductReviewDto>(Error.Validation(ex.Message));
        }

        await reviewsRepository.AddReviewAsync(review, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(MapReview(review));
    }

    public async Task<Result<ProductReviewDto>> UpdateOwnAsync(
        int customerId,
        int reviewId,
        UpdateProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var review = await reviewsRepository.GetReviewByIdAsync(reviewId, cancellationToken).ConfigureAwait(false);
        if (review is null || review.StoreId != storeId)
        {
            return Result.Failure<ProductReviewDto>(Error.NotFound("Review not found."));
        }

        if (!review.IsOwnedBy(customerId))
        {
            return Result.Failure<ProductReviewDto>(Error.Forbidden("You cannot modify this review."));
        }

        try
        {
            review.UpdateByCustomer(request.Rating, request.Title, request.Content, DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ProductReviewDto>(Error.Forbidden(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductReviewDto>(Error.Validation(ex.Message));
        }

        await reviewsRepository.SaveReviewAsync(review, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(MapReview(review));
    }

    private async Task<ProductRatingSummaryDto> BuildSummaryAsync(
        int productId,
        int storeId,
        CancellationToken cancellationToken)
    {
        var aggregate = await reviewsRepository
            .GetRatingAggregateAsync(productId, storeId, cancellationToken)
            .ConfigureAwait(false);

        return new ProductRatingSummaryDto(
            aggregate.AverageRating,
            aggregate.RatingCount,
            aggregate.Distribution);
    }

    private int RequireStoreId() =>
        storeContext.CurrentStoreId ?? throw new InvalidOperationException("Store context is required.");

    private static ProductReviewDto MapReview(ProductReview review) =>
        new(
            review.Id,
            review.ProductId,
            review.CustomerId,
            review.Rating,
            review.Title,
            review.Content,
            review.ModerationStatus,
            review.IsVerifiedPurchase,
            review.CreatedAtUtc,
            review.UpdatedAtUtc);
}
