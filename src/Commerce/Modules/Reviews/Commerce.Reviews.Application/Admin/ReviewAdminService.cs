using Commerce.Catalog.Contracts.Products;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Reviews.Application.Abstractions;
using Commerce.Reviews.Contracts.Admin;
using Commerce.Reviews.Domain.Entities;

namespace Commerce.Reviews.Application.Admin;

public sealed class ReviewAdminService(
    IReviewsRepository reviewsRepository,
    IProductReader productReader,
    ICustomerReader customerReader) : IReviewAdminService
{
    public async Task<Result<AdminReviewListDto>> ListAsync(
        int? storeId,
        int? productId,
        Domain.Enums.ReviewModerationStatus? moderationStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var criteria = new ReviewListCriteria(
            storeId,
            ProductId: productId,
            ModerationStatus: moderationStatus,
            Page: page,
            PageSize: pageSize);

        var (items, totalCount) = await reviewsRepository
            .ListReviewsAsync(criteria, cancellationToken)
            .ConfigureAwait(false);

        var dtos = new List<AdminProductReviewDto>();
        foreach (var review in items)
        {
            dtos.Add(await MapReviewAsync(review, cancellationToken).ConfigureAwait(false));
        }

        return Result.Success(new AdminReviewListDto(dtos, totalCount, page, pageSize));
    }

    public async Task<Result<AdminProductReviewDto>> GetAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await reviewsRepository.GetReviewByIdAsync(reviewId, cancellationToken).ConfigureAwait(false);
        if (review is null)
        {
            return Result.Failure<AdminProductReviewDto>(Error.NotFound("Review not found."));
        }

        return Result.Success(await MapReviewAsync(review, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> ApproveAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await reviewsRepository.GetReviewByIdAsync(reviewId, cancellationToken).ConfigureAwait(false);
        if (review is null)
        {
            return Result.Failure(Error.NotFound("Review not found."));
        }

        review.Approve(DateTime.UtcNow);
        await reviewsRepository.SaveReviewAsync(review, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> RejectAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await reviewsRepository.GetReviewByIdAsync(reviewId, cancellationToken).ConfigureAwait(false);
        if (review is null)
        {
            return Result.Failure(Error.NotFound("Review not found."));
        }

        review.Reject(DateTime.UtcNow);
        await reviewsRepository.SaveReviewAsync(review, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await reviewsRepository.GetReviewByIdAsync(reviewId, cancellationToken).ConfigureAwait(false);
        if (review is null)
        {
            return Result.Failure(Error.NotFound("Review not found."));
        }

        await reviewsRepository.DeleteReviewAsync(review, cancellationToken).ConfigureAwait(false);
        await reviewsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<AdminProductReviewDto> MapReviewAsync(ProductReview review, CancellationToken cancellationToken)
    {
        string? productName = null;
        var productResult = await productReader.GetByIdAsync(review.ProductId, cancellationToken).ConfigureAwait(false);
        if (productResult.IsSuccess)
        {
            productName = productResult.Value!.Name;
        }

        string? customerName = null;
        var customerResult = await customerReader.GetByIdAsync(review.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customerResult.IsSuccess)
        {
            var customer = customerResult.Value!;
            customerName = $"{customer.FirstName} {customer.LastName}".Trim();
        }

        return new AdminProductReviewDto(
            review.Id,
            review.ProductId,
            productName,
            review.CustomerId,
            customerName,
            review.StoreId,
            review.Rating,
            review.Title,
            review.Content,
            review.ModerationStatus,
            review.IsVerifiedPurchase,
            review.CreatedAtUtc,
            review.UpdatedAtUtc);
    }
}
